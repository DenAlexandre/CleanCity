using CortexiaAuth.Api.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CortexiaAuth.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    // Persiste les cles Data Protection en base : le systeme de fichiers de Render est ephemere,
    // sans ca les cles (et donc CortexiaPasswordProtected) seraient perdues a chaque redeploiement/restart.
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<AccessTokenRecord> AccessTokens => Set<AccessTokenRecord>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RoadEdge> RoadEdges => Set<RoadEdge>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<PointOfInterest> PointsOfInterest => Set<PointOfInterest>();
    public DbSet<EdgeSnapshot> EdgeSnapshots => Set<EdgeSnapshot>();
    public DbSet<EdgeCciMeasurement> EdgeCciMeasurements => Set<EdgeCciMeasurement>();
    public DbSet<ImportCheckpoint> ImportCheckpoints => Set<ImportCheckpoint>();
    public DbSet<DetectionDisplaySettings> DetectionDisplaySettings => Set<DetectionDisplaySettings>();
    public DbSet<WeatherSettings> WeatherSettings => Set<WeatherSettings>();
    public DbSet<PointOfInterestSettings> PointOfInterestSettings => Set<PointOfInterestSettings>();
    public DbSet<AlarmThreshold> AlarmThresholds => Set<AlarmThreshold>();
    public DbSet<AlarmEmailRecipient> AlarmEmailRecipients => Set<AlarmEmailRecipient>();
    public DbSet<Alarm> Alarms => Set<Alarm>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<ImportCheckpoint>(entity =>
        {
            entity.HasKey(e => e.Dataset);
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasOne(e => e.Role).WithMany().HasForeignKey(e => e.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.OwnsOne(e => e.Permissions);
        });

        modelBuilder.Entity<RoadEdge>(entity =>
        {
            entity.HasKey(e => new { e.U, e.V, e.Key });
            entity.Property(e => e.PropertiesJson).HasColumnType("jsonb");
            entity.Property(e => e.Geometry).HasColumnType("geometry");
            entity.HasIndex(e => e.Geometry).HasMethod("GIST");
        });

        modelBuilder.Entity<AlarmThreshold>(entity =>
        {
            entity.HasIndex(e => e.TypeCode).IsUnique();
        });

        modelBuilder.Entity<AlarmEmailRecipient>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<Alarm>(entity =>
        {
            entity.HasIndex(e => new { e.SnapshotId, e.TypeCode }).IsUnique();
        });

        modelBuilder.Entity<Place>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Geometry).HasMethod("GIST");
        });

        modelBuilder.Entity<PointOfInterest>(entity =>
        {
            entity.Property(e => e.Location).HasColumnType("geography(Point,4326)");
            entity.HasIndex(e => e.Location).HasMethod("GIST");
        });

        modelBuilder.Entity<EdgeSnapshot>(entity =>
        {
            // Clé primaire composite (Id, MeasuredAt) : Postgres exige que la colonne de partitionnement
            // fasse partie de toute clé primaire/unique sur une table partitionnée par RANGE.
            entity.HasKey(e => new { e.Id, e.MeasuredAt });
            entity.Property(e => e.Id).UseIdentityByDefaultColumn();
            entity.Property(e => e.Location).HasColumnType("geography(Point,4326)");
            entity.HasIndex(e => e.Location).HasMethod("GIST");
            entity.HasIndex(e => new { e.EdgeU, e.EdgeV, e.EdgeKey, e.MeasuredAt });
            entity.HasIndex(e => new { e.PlaceId, e.MeasuredAt }).HasFilter("\"PlaceId\" IS NOT NULL");
            entity.HasIndex(e => e.MeasuredAt).HasMethod("BRIN");
            entity.HasIndex(e => e.CityId);
        });

        modelBuilder.Entity<EdgeCciMeasurement>(entity =>
        {
            entity.HasKey(e => new { e.Id, e.MeasuredAt });
            entity.Property(e => e.Id).UseIdentityByDefaultColumn();
            entity.Property(e => e.CustomCciPerTypesJson).HasColumnType("jsonb");
            entity.HasIndex(e => new { e.EdgeU, e.EdgeV, e.EdgeKey, e.MeasuredAt });
            entity.HasIndex(e => new { e.PlaceId, e.MeasuredAt }).HasFilter("\"PlaceId\" IS NOT NULL");
            entity.HasIndex(e => e.MeasuredAt).HasMethod("BRIN");
        });
    }
}
