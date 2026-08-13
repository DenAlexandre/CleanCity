using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using CortexiaAuth.Api.Services;
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
var npgsqlDataSource = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("Default"))
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

builder.Services.AddDataProtection();
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