using AuthAPI.DTOs;
using AuthAPI.Helpers;
using AuthAPI.Models;
using AuthAPI.Repositories;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Role = AuthAPI.Models.Role;

namespace AuthAPI.Services.Implements
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IPasswordHelper _passwordHelper;
        private readonly IJwtHelper _jwtHelper;
        private readonly IMapper _mapper;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IEmailService _emailService;
        private readonly ISocialAuthService _socialAuthService;
        private readonly IOtpCacheService _otpCacheService;
        private readonly IEmailTemplateService _emailTemplateService;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHelper passwordHelper,
            IJwtHelper jwtHelper,
            IMapper mapper,
            ICloudinaryService cloudinaryService,
            IEmailService emailService,
            ISocialAuthService socialAuthService,
            IOtpCacheService otpCacheService,
            IEmailTemplateService emailTemplateService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHelper = passwordHelper;
            _jwtHelper = jwtHelper;
            _mapper = mapper;
            _cloudinaryService = cloudinaryService;
            _emailService = emailService;
            _socialAuthService = socialAuthService;
            _otpCacheService = otpCacheService;
            _emailTemplateService = emailTemplateService;
        }

        // 1. ĐĂNG NHẬP TRUYỀN THỐNG (LOCAL)
        public async Task<LoginResponseDTO?> Login(LoginDTO loginDTO)
        {
            var user = await _userRepository.GetByEmail(loginDTO.Email);

            if (user == null)
                return null;

            if (user.Provider != "Local")
                throw new InvalidOperationException("InvalidProvider");

            if (user.Status != "Active")
                return null;

            if (!_passwordHelper.Verify(user, user.PasswordHash, loginDTO.Password))
                return null;

            return await GenerateLoginResponse(user);
        }

        // 2. LÀM MỚI TOKEN (REFRESH TOKEN ROTATION)
        public async Task<LoginResponseDTO?> RefreshToken(string token)
        {
            var savedToken = await _refreshTokenRepository.GetByToken(token);
            if (savedToken == null)
                return null;

            if (savedToken.RevokedAt != null)
            {
                await _refreshTokenRepository.DeleteAllByUserId(savedToken.UserId);
                return null;
            }

            if (savedToken.Expires < DateTime.UtcNow)
                return null;

            var user = await _userRepository.GetById(savedToken.UserId);
            if (user == null || user.Status != "Active")
                return null;

            savedToken.RevokedAt = DateTime.UtcNow;
            savedToken.ReasonRevoked = "Replaced by new token";
            await _refreshTokenRepository.Update(savedToken);

            return await GenerateLoginResponse(user);
        }

        // 3a. GỬI OTP ĐĂNG KÝ
        public async Task<ForgotPasswordResultDTO> SendRegisterOtp(SendRegisterOtpDTO dto)
        {
            const int cooldownSeconds = 60;
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            if (await _userRepository.GetByEmail(normalizedEmail) != null)
            {
                return new ForgotPasswordResultDTO { IsSocialAccount = true, Provider = "Existing" };
            }

            var acquiredCooldown = await _otpCacheService.TrySetRegisterCooldownAsync(
                normalizedEmail,
                TimeSpan.FromSeconds(cooldownSeconds));

            if (!acquiredCooldown)
            {
                var remaining = await _otpCacheService.GetRegisterCooldownRemainingAsync(normalizedEmail);
                return new ForgotPasswordResultDTO
                {
                    IsRateLimited = true,
                    RetryAfterSeconds = Math.Max(
                        1,
                        (int)Math.Ceiling(remaining?.TotalSeconds ?? cooldownSeconds))
                };
            }

            string otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
            await _otpCacheService.SetRegisterOtpAsync(normalizedEmail, otp, TimeSpan.FromMinutes(10));

            string subject = "StayHub Account Registration Verification Code";
            string emailBody = _emailTemplateService.GenerateRegisterOtpEmailBody(dto.FullName, otp);

            try
            {
                await _emailService.SendEmailAsync(normalizedEmail, subject, emailBody);
            }
            catch
            {
                await _otpCacheService.DeleteRegisterOtpAsync(normalizedEmail);
                await _otpCacheService.DeleteRegisterCooldownAsync(normalizedEmail);
                throw;
            }

            return new ForgotPasswordResultDTO();
        }

        // 3b. ĐĂNG KÝ
        public async Task<UserResponseDTO?> Register(RegisterDTO registerDTO)
        {
            var normalizedEmail = registerDTO.Email.Trim().ToLowerInvariant();
            if (await _userRepository.GetByEmail(normalizedEmail) != null)
                return null;

            if (await _userRepository.GetByPhoneNumber(registerDTO.PhoneNumber) != null)
                throw new InvalidOperationException("PhoneNumberExists");


            var storedOtp = await _otpCacheService.GetRegisterOtpAsync(normalizedEmail);
            if (string.IsNullOrEmpty(storedOtp) || storedOtp != registerDTO.OtpCode)
                throw new InvalidOperationException("InvalidOrExpiredOtp");

            var newUser = _mapper.Map<User>(registerDTO);
            newUser.Email = normalizedEmail;
            newUser.PasswordHash = _passwordHelper.Hash(newUser, registerDTO.Password);
            newUser.Status = "Active";
            newUser.Provider = "Local";
            newUser.AvatarUrl = null;
            newUser.LocPrivacy = true;
            newUser.MomentPrivacy = true;
            newUser.SecurityStamp = Guid.NewGuid().ToString();
            newUser.CreatedAt = DateTime.UtcNow;
            newUser.UpdatedAt = DateTime.UtcNow;

            newUser.Roles ??= new List<Role>();

            var defaultRole = await _roleRepository.GetByName("Customer");
            if (defaultRole != null)
                newUser.Roles.Add(defaultRole);

            await _userRepository.Add(newUser);

            await _otpCacheService.DeleteRegisterOtpAsync(normalizedEmail);
            await _otpCacheService.DeleteRegisterCooldownAsync(normalizedEmail);

            return _mapper.Map<UserResponseDTO>(newUser);
        }

        // 4. ĐĂNG NHẬP GOOGLE
        public async Task<LoginResponseDTO?> GoogleLogin(GoogleLoginDTO googleLoginDTO)
        {
            var result = await _socialAuthService.ValidateGoogleTokenAsync(googleLoginDTO.IdToken);
            if (result == null)
                return null;

            return await ProcessSocialLogin(result.Email, result.Name, result.AvatarUrl, "Google", googleLoginDTO.PhoneNumber);
        }

        // 5. ĐĂNG NHẬP FACEBOOK
        public async Task<LoginResponseDTO?> FacebookLogin(string accessToken)
        {
            var result = await _socialAuthService.ValidateFacebookTokenAsync(accessToken);
            if (result == null)
                return null;

            return await ProcessSocialLogin(result.Email, result.Name, result.AvatarUrl, "Facebook");
        }

        // 6. ĐĂNG XUẤT
        public async Task Logout(string refreshToken)
        {
            var token = await _refreshTokenRepository.GetByToken(refreshToken);
            if (token != null && token.RevokedAt == null)
            {
                token.RevokedAt = DateTime.UtcNow;
                token.ReasonRevoked = "User logged out";
                await _refreshTokenRepository.Update(token);
            }
        }

        // 7. ĐỔI MẬT KHẨU
        public async Task<LoginResponseDTO?> ChangePassword(int userId, ChangePasswordDTO changePasswordDTO)
        {
            var user = await _userRepository.GetById(userId);

            if (user == null || user.Provider != "Local" || user.Status != "Active")
                return null;

            if (!_passwordHelper.Verify(user, user.PasswordHash, changePasswordDTO.OldPassword))
                return null;

            user.PasswordHash = _passwordHelper.Hash(user, changePasswordDTO.NewPassword);
            user.RequirePasswordChange = false;
            user.SecurityStamp = Guid.NewGuid().ToString(); // Vô hiệu hoá nội bộ bên AuthAPI
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.Update(user.Id, user);

            await _refreshTokenRepository.DeleteAllByUserId(user.Id);

            await _otpCacheService.RevokeUserSessionAsync(user.Id, TimeSpan.FromMinutes(60));

            return await GenerateLoginResponse(user);
        }

        // 8. QUÊN MẬT KHẨU
        public async Task<ForgotPasswordResultDTO> ForgotPassword(ForgotPasswordDTO dto)
        {
            const int cooldownSeconds = 60;
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

            var acquiredCooldown = await _otpCacheService.TrySetForgotPasswordCooldownAsync(
                normalizedEmail,
                TimeSpan.FromSeconds(cooldownSeconds));

            if (!acquiredCooldown)
            {
                var remaining = await _otpCacheService.GetForgotPasswordCooldownRemainingAsync(normalizedEmail);
                return new ForgotPasswordResultDTO
                {
                    IsRateLimited = true,
                    RetryAfterSeconds = Math.Max(
                        1,
                        (int)Math.Ceiling(remaining?.TotalSeconds ?? cooldownSeconds))
                };
            }

            var user = await _userRepository.GetByEmail(normalizedEmail);

            if (user == null || user.Status != "Active")
                return new ForgotPasswordResultDTO();

            if (user.Provider != "Local")
                return new ForgotPasswordResultDTO { IsSocialAccount = true, Provider = user.Provider };

            string otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            await _otpCacheService.SetForgotPasswordOtpAsync(normalizedEmail, otp, TimeSpan.FromMinutes(5));

            string subject = "Your Password Reset Verification Code";
            string emailBody = _emailTemplateService.GenerateForgotPasswordEmailBody(user.FullName, otp);

            try
            {
                await _emailService.SendEmailAsync(normalizedEmail, subject, emailBody);
            }
            catch
            {
                await _otpCacheService.DeleteForgotPasswordOtpAsync(normalizedEmail);
                await _otpCacheService.DeleteForgotPasswordCooldownAsync(normalizedEmail);
                throw;
            }

            return new ForgotPasswordResultDTO();
        }

        // 9a. XÁC MINH OTP ĐẶT LẠI MẬT KHẨU (TẠO RESET TOKEN BẢO MẬT)
        public async Task<string?> VerifyResetOtp(VerifyResetOtpDTO dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var storedOtp = await _otpCacheService.GetForgotPasswordOtpAsync(normalizedEmail);

            if (string.IsNullOrEmpty(storedOtp) || storedOtp != dto.Code)
                return null;

            var user = await _userRepository.GetByEmail(normalizedEmail);
            if (user == null || user.Provider != "Local" || user.Status != "Active")
                return null;

            await _otpCacheService.DeleteForgotPasswordOtpAsync(normalizedEmail);

            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            string resetToken = Convert.ToHexString(randomNumber).ToLowerInvariant();

            await _otpCacheService.SetResetPasswordTokenAsync(normalizedEmail, resetToken, TimeSpan.FromMinutes(15));

            return resetToken;
        }

        // 9b. ĐẶT LẠI MẬT KHẨU
        public async Task<bool> ResetPassword(ResetPasswordDTO dto)
        {
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var tokenInput = dto.ResetToken ?? dto.Code;

            if (string.IsNullOrEmpty(tokenInput))
                return false;

            var storedResetToken = await _otpCacheService.GetResetPasswordTokenAsync(normalizedEmail);
            var storedOtp = await _otpCacheService.GetForgotPasswordOtpAsync(normalizedEmail);

            bool isValidToken = (!string.IsNullOrEmpty(storedResetToken) && storedResetToken == tokenInput)
                             || (!string.IsNullOrEmpty(storedOtp) && storedOtp == tokenInput);

            if (!isValidToken)
                return false;

            var user = await _userRepository.GetByEmail(normalizedEmail);
            if (user == null || user.Provider != "Local" || user.Status != "Active")
                return false;

            user.PasswordHash = _passwordHelper.Hash(user, dto.NewPassword);
            user.RequirePasswordChange = false;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.Update(user.Id, user);

            await _refreshTokenRepository.DeleteAllByUserId(user.Id);

            await _otpCacheService.RevokeUserSessionAsync(user.Id, TimeSpan.FromMinutes(60));
            await _otpCacheService.DeleteForgotPasswordOtpAsync(normalizedEmail);
            await _otpCacheService.DeleteResetPasswordTokenAsync(normalizedEmail);

            return true;
        }

        // 10. LẤY HỒ SƠ CÁ NHÂN
        public async Task<UserResponseDTO?> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetById(userId);

            if (user == null || user.Status != "Active")
                return null;

            return _mapper.Map<UserResponseDTO>(user);
        }

        // 11. CẬP NHẬT HỒ SƠ
        public async Task<LoginResponseDTO?> UpdateProfileAsync(int userId, UpdateProfileDTO dto)
        {
            var user = await _userRepository.GetById(userId);

            if (user == null || user.Status != "Active")
                return null;

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
            {
                var existingPhoneUser = await _userRepository.GetByPhoneNumber(dto.PhoneNumber);
                if (existingPhoneUser != null)
                {
                    throw new InvalidOperationException("PhoneNumberExists");
                }
            }

            if (dto.AvatarFile != null)
            {
                if (!string.IsNullOrEmpty(user.AvatarUrl))
                {
                    var oldPublicId = _cloudinaryService.ExtractPublicIdFromUrl(user.AvatarUrl);
                    if (oldPublicId != null)
                    {
                        await _cloudinaryService.DeleteImageAsync(oldPublicId);
                    }
                }

                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.AvatarFile, "StayHub_Avatars");
                if (uploadResult.Error == null)
                {
                    user.AvatarUrl = uploadResult.SecureUrl.ToString();
                }
            }

            _mapper.Map(dto, user);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.Update(userId, user);

            return await GenerateLoginResponse(user);
        }

        private async Task<LoginResponseDTO?> ProcessSocialLogin(string email, string name, string? avatarUrl, string provider, string? phoneNumber = null)
        {
            var user = await _userRepository.GetByEmail(email);

            if (user == null || string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return new LoginResponseDTO
                    {
                        RequirePhoneNumber = true,
                        User = new UserResponseDTO
                        {
                            Email = email,
                            FullName = name ?? user?.FullName ?? $"{provider} User",
                            AvatarUrl = avatarUrl ?? user?.AvatarUrl,
                            Provider = provider
                        },
                        Token = string.Empty,
                        RefreshToken = string.Empty
                    };
                }

                if (phoneNumber.Length > 15)
                {
                    throw new ArgumentException("PhoneNumberMax15Chars");
                }
                
                var existingPhoneUser = await _userRepository.GetByPhoneNumber(phoneNumber);
                if (existingPhoneUser != null && existingPhoneUser.Email != email)
                {
                    throw new InvalidOperationException("PhoneNumberExists");
                }

                if (user == null)
                {
                    user = CreateSocialUser(email, name, avatarUrl, provider, phoneNumber);
                    var defaultRole = await _roleRepository.GetByName("Customer");
                    if (defaultRole != null) user.Roles.Add(defaultRole);

                    await _userRepository.Add(user);
                }
                else
                {
                    if (user.Provider != provider)
                        throw new InvalidOperationException("InvalidProvider");

                    if (user.Status != "Active")
                        return null;

                    user.PhoneNumber = phoneNumber;
                    if (user.AvatarUrl != avatarUrl && !string.IsNullOrEmpty(avatarUrl))
                    {
                        user.AvatarUrl = avatarUrl;
                    }
                    await _userRepository.Update(user.Id, user);
                }
            }
            else
            {
                if (user.Provider != provider)
                    throw new InvalidOperationException("InvalidProvider");

                if (user.Status != "Active")
                    return null;

                if (user.AvatarUrl != avatarUrl && !string.IsNullOrEmpty(avatarUrl))
                {
                    user.AvatarUrl = avatarUrl;
                    await _userRepository.Update(user.Id, user);
                }
            }

            return await GenerateLoginResponse(user);
        }

        private User CreateSocialUser(string email, string name, string? avatarUrl, string provider, string? phoneNumber = null)
        {
            return new User
            {
                Email = email,
                FullName = name ?? $"{provider} User",
                AvatarUrl = avatarUrl,
                PhoneNumber = phoneNumber,
                PasswordHash = string.Empty,
                Status = "Active",
                Provider = provider,
                LocPrivacy = true,
                MomentPrivacy = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Roles = new List<Role>(),
                RefreshTokens = new List<RefreshToken>()
            };
        }

        private async Task<LoginResponseDTO> GenerateLoginResponse(User user)
        {
            string accessToken = _jwtHelper.GenerateToken(user);

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = GenerateSecureRefreshToken(),
                Expires = DateTime.UtcNow.AddDays(3),
                CreatedAt = DateTime.UtcNow,
            };

            await _refreshTokenRepository.Add(refreshTokenEntity);

            return new LoginResponseDTO
            {
                User = _mapper.Map<UserResponseDTO>(user),
                Token = accessToken,
                RefreshToken = refreshTokenEntity.Token
            };
        }

        public async Task<bool> CompleteTourAsync(int userId)
        {
            var user = await _userRepository.GetById(userId);
            if (user == null)
                return false;

            user.HasCompletedTour = true;
            await _userRepository.Update(userId, user);
            return true;
        }

        private string GenerateSecureRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
