namespace ApplicationService.Clients;

public interface IIdentityClient
{
    Task<UserInfo?> GetUserInfoAsync(Guid userId);
}

public record UserInfo(Guid Id, string FullName, string Email);
