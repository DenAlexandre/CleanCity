namespace CortexiaAuth.Api.Services;

public class CortexiaTimeoutException() : Exception("Le délai d'attente de la réponse de Cortexia a été dépassé.");
