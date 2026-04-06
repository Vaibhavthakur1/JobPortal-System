namespace Shared.Contracts.Events.Identity;

public record EmailVerificationRequestedEvent(
    Guid UserId,
    string Email,
    string VerificationToken);
