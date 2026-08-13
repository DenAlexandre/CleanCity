namespace CortexiaAuth.Api.Services;

/// <summary>
/// Levée quand le fichier importé n'est pas un JSON valide ou ne respecte pas le format attendu
/// (structure Cortexia). Distincte des erreurs serveur : doit être renvoyée en 400, pas en 500.
/// </summary>
public class ImportValidationException(string message) : Exception(message);
