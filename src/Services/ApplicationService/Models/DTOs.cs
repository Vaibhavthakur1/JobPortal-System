namespace ApplicationService.Models;

public record SubmitApplicationRequest(Guid JobId, Guid ResumeId, string? CoverLetter);
public record UpdateStatusRequest(string NewStatus, string? Note);

public record ApplicationDto(
    Guid Id, Guid JobSeekerId, Guid JobId, Guid ResumeId,
    string Status, string? CoverLetter, bool IsWithdrawn,
    DateTime CreatedAt, List<StatusHistoryDto> History);

public record StatusHistoryDto(string FromStatus, string ToStatus, string? Note, DateTime ChangedAt);
