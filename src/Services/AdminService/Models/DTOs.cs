namespace AdminService.Models;

public record AuditLogDto(Guid Id, Guid? UserId, string Action, string Entity, string? EntityId, DateTime CreatedAt);
public record FlagJobRequest(Guid JobId, string Reason);
public record ReviewFlagRequest(string Status); // Reviewed | Dismissed
public record FlaggedJobDto(Guid Id, Guid JobId, Guid FlaggedBy, string Reason, string Status, DateTime CreatedAt);
public record UserSummaryDto(Guid Id, string FullName, string Email, string Role, bool IsActive, DateTime CreatedAt);
public record UpdateUserRoleRequest(string Role);
public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);
