namespace CortexiaAuth.Api.Models;

public record AlarmDto(
    long Id,
    DateTime MeasuredAt,
    string? Street,
    short TypeCode,
    string TypeName,
    int Count,
    int Threshold,
    bool EmailSent);

public record PagedAlarmsResponse(int Total, int Page, int PageSize, IReadOnlyList<AlarmDto> Items);
