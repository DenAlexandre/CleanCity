namespace CortexiaAuth.Api.Data;

/// <summary>
/// Table statique des types d'objets détectés par Cortexia (codes numériques stockés dans
/// EdgeSnapshots.Details). Cortexia n'expose pas cette correspondance via son API.
/// </summary>
public static class DetectionTypeCatalog
{
    public static readonly IReadOnlyDictionary<short, string> Names = new Dictionary<short, string>
    {
        [1] = "Cigarette",
        [2] = "Feuille",
        [3] = "Groupe de feuilles",
        [4] = "Papier carton",
        [5] = "Canette",
        [7] = "Bouteille en verre",
        [8] = "Bouteille en PET",
        [9] = "Berlingots/contenant en carton",
        [13] = "Emballages alimentaires",
        [14] = "Journaux",
        [16] = "Verre brisé",
        [17] = "Seringues",
        [36] = "Plastique transparent",
        [37] = "Plastique opaque",
        [40] = "Capsules",
        [61] = "Petits sacs pour déjections canines",
        [63] = "Masques",
    };

    public static string GetName(short code) => Names.TryGetValue(code, out var name) ? name : $"Type {code}";
}
