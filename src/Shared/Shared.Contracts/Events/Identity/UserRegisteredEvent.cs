namespace Shared.Contracts.Events.Identity;

public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    DateTime RegisteredAt);
