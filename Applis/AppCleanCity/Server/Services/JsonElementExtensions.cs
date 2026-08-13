using System.Text.Json;

namespace CortexiaAuth.Api.Services;

/// <summary>
/// Certains champs Cortexia (highway, name, ref, osmid, maxspeed...) sont tantôt un scalaire,
/// tantôt un tableau selon l'edge. Ces helpers uniformisent la lecture pour les colonnes typées,
/// la valeur brute restant de toute façon conservée dans la colonne jsonb "Properties".
/// </summary>
internal static class JsonElementExtensions
{
    public static string? GetFlatString(this JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Array => string.Join("; ", value.EnumerateArray().Select(e => e.ToString())),
            _ => value.ToString(),
        };
    }

    public static long[] GetLongOrArray(this JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var value) || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        return value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(e => e.GetInt64()).ToArray()
            : [value.GetInt64()];
    }

    public static float? GetNullableSingle(this JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetSingle()
            : null;
}
