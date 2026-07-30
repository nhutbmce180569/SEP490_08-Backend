namespace SystemAPI.Services
{
    public interface IAuthApiClient
    {
        Task<string?> GetFcmTokenAsync(int userId);
        Task ClearFcmTokenAsync(int userId);
    }
}
