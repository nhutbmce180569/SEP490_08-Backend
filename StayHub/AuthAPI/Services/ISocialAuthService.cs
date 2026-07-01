using AuthAPI.DTOs;
using System.Threading.Tasks;

namespace AuthAPI.Services
{
    public interface ISocialAuthService
    {
        Task<SocialAuthResultDTO?> ValidateGoogleTokenAsync(string idToken);
        Task<SocialAuthResultDTO?> ValidateFacebookTokenAsync(string accessToken);
    }
}
