namespace CortexiaAuth.Api.Models;

public record DetectionDisplaySettingsDto(
    double PositiveMin, double PositiveMax, string PositiveColor,
    double AverageMin, double AverageMax, string AverageColor,
    bool HideObjectsWithoutStreet);
