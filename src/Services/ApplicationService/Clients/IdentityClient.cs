using System.Net.Http.Json;

namespace ApplicationService.Clients;

public class IdentityClient(HttpClient http, ILogger<IdentityClient> logger) : IIdentityClient
{
    public async Task<UserInfo?> GetUserInfoAsync(Guid userId)
    {
        try
        {
            return await http.GetFromJsonAsync<UserInfo>($"internal/users/{userId}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch user info for {UserId}", userId);
            return null;
        }
    }
}
