using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using CortexiaAuth.Api.Services;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Cortexia Auth API",
        Version = "v1",
        Description = "API pour générer un access token Cortexia et interroger les données Cortexia.",
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Token obtenu via /api/Auth/token. Saisir uniquement le token (sans le préfixe 'Bearer').",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document, null), [] },
    });
});

// Data source partagé : EF Core et le service d'import (COPY binaire direct) utilisent
// la même configuration de mapping NetTopologySuite pour les colonnes geometry/geography.
var npgsqlDataSource = new NpgsqlDataSourceBuilder(NormalizeConnectionString(builder.Configuration.GetConnectionString("Default")))
    .UseNetTopologySuite()
    .Build();
builder.Services.AddSingleton(npgsqlDataSource);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(npgsqlDataSource, npgsql => npgsql.UseNetTopologySuite()));

var cortexiaTimeout = TimeSpan.FromSeconds(builder.Configuration.GetValue("Cortexia:TimeoutSeconds", 100));

builder.Services.AddHttpClient<ICortexiaAuthService, CortexiaAuthService>(client => 
{
    client.BaseAddress = new Uri(builder.Configuration["Cortexia:BaseUrl"]!);
    client.Timeout = cortexiaTimeout;
});

builder.Services.AddHttpClient<ICortexiaGeoService, CortexiaGeoService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Cortexia:BaseUrl"]!);
    client.Timeout = cortexiaTimeout;
});

builder.Services.AddScoped<IDataImportService, CortexiaDataImportService>();
builder.Services.AddHostedService<CortexiaImportBackgroundService>();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IAlarmEmailSender, SmtpAlarmEmailSender>();
builder.Services.AddScoped<IAlarmDetectionService, AlarmDetectionService>();
builder.Services.AddScoped<IServerTaskService, ServerTaskService>();

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>();
builder.Services.AddSingleton<ICortexiaCredentialProtector, CortexiaCredentialProtector>();
builder.Services.AddSingleton<PasswordHasher<AppUser>>();

const string FrontendCorsPolicy = "Frontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// Derriere le proxy TLS de Render (ou tout autre PaaS), la requete arrive en HTTP au conteneur ;
// sans ceci, UseHttpsRedirection ne voit jamais une requete HTTPS et boucle indefiniment. Les IP du
// proxy Render ne sont pas connues a l'avance, d'ou le Clear() (sinon seul le loopback est fiable).
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
};
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cortexia Auth API v1");
});

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

// Neon (et d'autres PaaS) fournissent la chaine de connexion au format URI (postgresql://user:pass@host/db?sslmode=require),
// alors que Npgsql attend le format cle=valeur. On convertit ici pour eviter de devoir reformater la variable
// d'environnement a la main a chaque redeploiement.
static string NormalizeConnectionString(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
        return connectionString ?? string.Empty;

    if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':', 2);

    var csBuilder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort ? 5432 : uri.Port,
        Database = uri.AbsolutePath.TrimStart('/'),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null,
    };

    foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        var kv = pair.Split('=', 2);
        var key = Uri.UnescapeDataString(kv[0]);
        var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : string.Empty;

        switch (key.ToLowerInvariant())
        {
            case "sslmode":
                csBuilder.SslMode = value.ToLowerInvariant() switch
                {
                    "disable" => SslMode.Disable,
                    "allow" => SslMode.Allow,
                    "prefer" => SslMode.Prefer,
                    "require" => SslMode.Require,
                    "verify-ca" or "verifyca" => SslMode.VerifyCA,
                    "verify-full" or "verifyfull" => SslMode.VerifyFull,
                    _ => csBuilder.SslMode,
                };
                break;
            case "channel_binding":
                csBuilder.ChannelBinding = value.ToLowerInvariant() switch
                {
                    "disable" => ChannelBinding.Disable,
                    "prefer" => ChannelBinding.Prefer,
                    "require" => ChannelBinding.Require,
                    _ => csBuilder.ChannelBinding,
                };
                break;
        }
    }

    return csBuilder.ConnectionString;
}