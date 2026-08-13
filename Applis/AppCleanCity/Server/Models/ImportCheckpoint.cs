namespace CortexiaAuth.Api.Models;

/// <summary>
/// Mémorise jusqu'à quand un jeu de données a été importé, pour ne récupérer que les données
/// nouvelles à chaque exécution périodique (plutôt que tout l'historique à chaque fois).
/// </summary>
public class ImportCheckpoint
{
    public string Dataset { get; set; } = string.Empty;
    public DateTime LastImportedUntilUtc { get; set; }
}
