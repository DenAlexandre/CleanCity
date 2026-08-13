using Npgsql;

namespace CortexiaAuth.Api.Services;

/// <summary>
/// Neon (et d'autres PaaS) fournissent la chaîne de connexion au format URI
/// (postgresql://user:pass@host/db?sslmode=require), alors que Npgsql (et pg_dump/psql) attendent le
/// format clé=valeur / des paramètres séparés. Utilisé au démarrage (Program.cs) ainsi que par
/// ExportController (pg_dump/psql), qui a besoin des mêmes composants (host, port, user...).
/// </summary>
public static class PostgresConnectionString
{
    public static string Normalize(string? connectionString)
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
}
