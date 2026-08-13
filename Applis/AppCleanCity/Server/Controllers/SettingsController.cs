using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Réglages configurables du site : seuils/couleurs des détections sur la carte, ville du bandeau
/// météo. Lecture libre, écriture réservée à ManageAccounts (même mécanisme d'authentification
/// que les autres contrôleurs admin).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class SettingsController(AppDbContext dbContext, PasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [HttpGet("detection-display")]
    [ProducesResponseType(typeof(DetectionDisplaySettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DetectionDisplaySettingsDto>> GetDetectionDisplay(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        return Ok(ToDto(settings));
    }

    [HttpPut("detection-display")]
    [ProducesResponseType(typeof(DetectionDisplaySettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DetectionDisplaySettingsDto>> UpdateDetectionDisplay(
        [FromBody] DetectionDisplaySettingsDto request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var settings = await GetOrCreateSettingsAsync(cancellationToken);
        settings.PositiveMin = request.PositiveMin;
        settings.PositiveMax = request.PositiveMax;
        settings.PositiveColor = request.PositiveColor;
        settings.AverageMin = request.AverageMin;
        settings.AverageMax = request.AverageMax;
        settings.AverageColor = request.AverageColor;
        settings.HideObjectsWithoutStreet = request.HideObjectsWithoutStreet;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(settings));
    }

    [HttpGet("weather")]
    [ProducesResponseType(typeof(WeatherSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WeatherSettingsDto>> GetWeather(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreateWeatherSettingsAsync(cancellationToken);
        return Ok(ToDto(settings));
    }

    [HttpPut("weather")]
    [ProducesResponseType(typeof(WeatherSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<WeatherSettingsDto>> UpdateWeather(
        [FromBody] WeatherSettingsDto request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var settings = await GetOrCreateWeatherSettingsAsync(cancellationToken);
        settings.City = request.City;
        settings.Latitude = request.Latitude;
        settings.Longitude = request.Longitude;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(settings));
    }

    [HttpGet("point-of-interest")]
    [ProducesResponseType(typeof(PointOfInterestSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PointOfInterestSettingsDto>> GetPointOfInterest(CancellationToken cancellationToken)
    {
        var settings = await GetOrCreatePointOfInterestSettingsAsync(cancellationToken);
        return Ok(ToDto(settings));
    }

    [HttpPut("point-of-interest")]
    [ProducesResponseType(typeof(PointOfInterestSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PointOfInterestSettingsDto>> UpdatePointOfInterest(
        [FromBody] PointOfInterestSettingsDto request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var settings = await GetOrCreatePointOfInterestSettingsAsync(cancellationToken);
        settings.RadiusMeters = request.RadiusMeters;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToDto(settings));
    }

    private async Task<DetectionDisplaySettings> GetOrCreateSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.DetectionDisplaySettings.SingleOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new DetectionDisplaySettings();
            dbContext.DetectionDisplaySettings.Add(settings);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }

    private async Task<WeatherSettings> GetOrCreateWeatherSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.WeatherSettings.SingleOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new WeatherSettings();
            dbContext.WeatherSettings.Add(settings);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }

    private async Task<PointOfInterestSettings> GetOrCreatePointOfInterestSettingsAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.PointOfInterestSettings.SingleOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = new PointOfInterestSettings();
            dbContext.PointOfInterestSettings.Add(settings);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return settings;
    }

    private static DetectionDisplaySettingsDto ToDto(DetectionDisplaySettings settings) => new(
        settings.PositiveMin, settings.PositiveMax, settings.PositiveColor,
        settings.AverageMin, settings.AverageMax, settings.AverageColor,
        settings.HideObjectsWithoutStreet);

    private static WeatherSettingsDto ToDto(WeatherSettings settings) => new(settings.City, settings.Latitude, settings.Longitude);

    private static PointOfInterestSettingsDto ToDto(PointOfInterestSettings settings) => new(settings.RadiusMeters);

    /// <summary>
    /// Authentifie l'appelant comme administrateur via les headers X-Admin-Username / X-Admin-Password
    /// (même contrat que les autres contrôleurs : pas de session/JWT côté site).
    /// </summary>
    private async Task<ActionResult?> AuthenticateAdminAsync(string? adminUsername, string? adminPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword))
        {
            return Unauthorized(new { error = "Authentification administrateur requise (headers X-Admin-Username / X-Admin-Password)." });
        }

        var admin = await dbContext.AppUsers.Include(u => u.Role).SingleOrDefaultAsync(u => u.Username == adminUsername, cancellationToken);
        if (admin is null || passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, adminPassword) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { error = $"Aucun compte administrateur ne correspond à l'identifiant '{adminUsername}' avec ce mot de passe." });
        }

        if (!admin.Role.Permissions.ManageAccounts)
        {
            return Unauthorized(new { error = $"Le compte '{adminUsername}' existe mais n'a pas le droit 'Gestion des comptes'." });
        }

        return null;
    }
}
