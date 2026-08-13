namespace CortexiaAuth.Api.Models;

public record CleanlinessScoreDto(double? CurrentAverage, double? PreviousAverage);

public record CleanlinessHistoryPointDto(DateOnly WeekStart, double AverageCci);

public record DirtiestStreetDto(string Street, double AverageCci);

public record PointOfInterestCategoryScoreDto(string Category, double? AverageCci, int PoiCount);

public record UrgentAlertDto(DateTime MeasuredAt, string? Street, short TypeCode, string TypeName, int Count, int Threshold);
