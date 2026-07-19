using AuthAPI.DTOs;

namespace AuthAPI.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDTO?> Login(LoginDTO loginDTO);
        Task<LoginResponseDTO?> RefreshToken(string token);
        Task<ForgotPasswordResultDTO> SendRegisterOtp(SendRegisterOtpDTO dto);
        Task<UserResponseDTO?> Register(RegisterDTO registerDTO);
        Task<LoginResponseDTO?> GoogleLogin(GoogleLoginDTO googleLoginDTO);
        Task<LoginResponseDTO?> FacebookLogin(string accessToken);
        Task Logout(string refreshToken);
        Task<LoginResponseDTO?> ChangePassword(int userId, ChangePasswordDTO changePasswordDTO);
        Task<ForgotPasswordResultDTO> ForgotPassword(ForgotPasswordDTO dto);
        Task<string?> VerifyResetOtp(VerifyResetOtpDTO dto);
        Task<bool> ResetPassword(ResetPasswordDTO dto);
        Task<UserResponseDTO?> GetProfileAsync(int userId);
        Task<LoginResponseDTO?> UpdateProfileAsync(int userId, UpdateProfileDTO dto);

    }
}
