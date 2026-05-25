using AuthAPI.DTOs;

namespace AuthAPI.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDTO?> Login(LoginDTO loginDTO);
        Task<LoginResponseDTO?> RefreshToken(string token);
        Task<UserResponseDTO?> Register(RegisterDTO registerDTO);
        Task<LoginResponseDTO?> GoogleLogin(GoogleLoginDTO googleLoginDTO);
        Task<LoginResponseDTO?> FacebookLogin(string accessToken);
        Task Logout(string refreshToken);
        Task<LoginResponseDTO?> ChangePassword(int userId, ChangePasswordDTO changePasswordDTO);
        Task<bool> ForgotPassword(ForgotPasswordDTO dto);
        Task<bool> ResetPassword(ResetPasswordDTO dto);
        Task<UserResponseDTO?> GetProfileAsync(int userId);
        Task<LoginResponseDTO?> UpdateProfileAsync(int userId, UpdateProfileDTO dto);

    }
}