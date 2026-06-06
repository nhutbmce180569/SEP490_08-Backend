using AuthAPI.DTOs;
using AuthAPI.Helpers;
using AuthAPI.Models;
using AuthAPI.Repositories;
using AutoMapper;
using Google.Apis.Auth;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using StackExchange.Redis;
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
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConnectionMultiplexer _redis;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IEmailService _emailService;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IPasswordHelper passwordHelper,
            IJwtHelper jwtHelper,
            IMapper mapper,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IConnectionMultiplexer redis,
            ICloudinaryService cloudinaryService,
            IEmailService emailService)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _passwordHelper = passwordHelper;
            _jwtHelper = jwtHelper;
            _mapper = mapper;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _redis = redis;
            _cloudinaryService = cloudinaryService;
            _emailService = emailService;
        }

        // 1. ĐĂNG NHẬP TRUYỀN THỐNG (LOCAL)
        public async Task<LoginResponseDTO?> Login(LoginDTO loginDTO)
        {
            var user = await _userRepository.GetByEmail(loginDTO.Email);

            if (user == null || user.Provider != "Local" || user.Status != "Active")
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

        public async Task<UserResponseDTO?> Register(RegisterDTO registerDTO)
        {
            if (await _userRepository.GetByEmail(registerDTO.Email) != null)
                return null;

            var newUser = _mapper.Map<User>(registerDTO);
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
            return _mapper.Map<UserResponseDTO>(newUser);
        }

        // 4. ĐĂNG NHẬP GOOGLE
        public async Task<LoginResponseDTO?> GoogleLogin(GoogleLoginDTO googleLoginDTO)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new List<string> { _configuration["Google:ClientId"]! }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(googleLoginDTO.IdToken, settings);
            }
            catch (InvalidJwtException)
            {
                return null;
            }

            return await ProcessSocialLogin(payload.Email, payload.Name, payload.Picture, "Google");
        }

        public async Task<LoginResponseDTO?> FacebookLogin(string accessToken)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"https://graph.facebook.com/me?fields=id,email,name,picture.type(large)&access_token={accessToken}");

            if (!response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync();
            var fbData = JObject.Parse(content);

            string? email = fbData["email"]?.ToString();
            if (string.IsNullOrEmpty(email))
                return null;

            string? name = fbData["name"]?.ToString();
            string? avatarUrl = fbData["picture"]?["data"]?["url"]?.ToString();

            return await ProcessSocialLogin(email, name ?? "Facebook User", avatarUrl, "Facebook");
        }

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

            var db = _redis.GetDatabase();
            long currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string redisKey = $"revoke_user_{userId}";

            await db.StringSetAsync(redisKey, currentUnixTimestamp, TimeSpan.FromMinutes(60));

            return await GenerateLoginResponse(user);
        }


        public async Task<ForgotPasswordResultDTO> ForgotPassword(ForgotPasswordDTO dto)
        {
            const int cooldownSeconds = 60;
            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var db = _redis.GetDatabase();
            var cooldownKey = $"forgot_pwd_cooldown_{normalizedEmail}";
            var acquiredCooldown = await db.StringSetAsync(
                cooldownKey,
                "1",
                TimeSpan.FromSeconds(cooldownSeconds),
                When.NotExists);

            if (!acquiredCooldown)
            {
                var remaining = await db.KeyTimeToLiveAsync(cooldownKey);
                return new ForgotPasswordResultDTO
                {
                    IsRateLimited = true,
                    RetryAfterSeconds = Math.Max(
                        1,
                        (int)Math.Ceiling(remaining?.TotalSeconds ?? cooldownSeconds))
                };
            }

            var user = await _userRepository.GetByEmail(normalizedEmail);

            if (user == null || user.Provider != "Local" || user.Status != "Active")
                return new ForgotPasswordResultDTO();

            string otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            string redisKey = $"forgot_pwd_otp_{normalizedEmail}";
            await db.StringSetAsync(redisKey, otp, TimeSpan.FromMinutes(5));

            string subject = "Your Password Reset Verification Code";

            string emailBody = $@"
    <div style='font-family: ""Helvetica Neue"", Helvetica, Arial, sans-serif; background-color: #f4f5f7; padding: 40px 20px; color: #333333;'>
        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 40px; border-radius: 8px; box-shadow: 0 4px 10px rgba(0,0,0,0.05);'>
            
            <h2 style='color: #2c3e50; text-align: center; border-bottom: 2px solid #f0f2f5; padding-bottom: 20px; margin-top: 0;'>Password Reset Request</h2>
            
            <p style='font-size: 16px; line-height: 1.6; margin-top: 20px;'>Hello <strong>{user.FullName}</strong>,</p>
            
            <p style='font-size: 16px; line-height: 1.6;'>We received a request to reset the password for your account associated with this email address. Please use the verification code below to proceed:</p>
            
            <div style='text-align: center; margin: 35px 0;'>
                <span style='font-size: 32px; font-weight: bold; color: #0056b3; letter-spacing: 8px; padding: 15px 30px; background-color: #f8f9fa; border-radius: 8px; border: 2px dashed #0056b3; display: inline-block;'>{otp}</span>
            </div>
            
            <p style='font-size: 15px; color: #e74c3c; text-align: center; font-weight: bold; margin-bottom: 30px;'>
                ⏱️ This code is valid for exactly 5 minutes.
            </p>
            
            <p style='font-size: 14px; line-height: 1.6; color: #666666;'>
                If you did not request a password reset, you can safely ignore this email. Your password will remain unchanged, and your account is secure.
            </p>
            
            <hr style='border: none; border-top: 1px solid #eeeeee; margin: 30px 0;' />
            
            <p style='font-size: 13px; color: #999999; text-align: center; margin-bottom: 0;'>
                Best regards,<br>
                <strong>The StayHub Team</strong>
            </p>
        </div>
    </div>";

            try
            {
                await _emailService.SendEmailAsync(normalizedEmail, subject, emailBody);
            }
            catch
            {
                await db.KeyDeleteAsync(new RedisKey[] { redisKey, cooldownKey });
                throw;
            }

            return new ForgotPasswordResultDTO();
        }

        public async Task<bool> ResetPassword(ResetPasswordDTO dto)
        {
            var db = _redis.GetDatabase();
            string redisKey = $"forgot_pwd_otp_{dto.Email.Trim().ToLowerInvariant()}";
            var storedOtp = await db.StringGetAsync(redisKey);

            if (storedOtp.IsNullOrEmpty || storedOtp.ToString() != dto.Code)
                return false;

            var user = await _userRepository.GetByEmail(dto.Email);
            if (user == null || user.Provider != "Local" || user.Status != "Active")
                return false;

            user.PasswordHash = _passwordHelper.Hash(user, dto.NewPassword);
            user.RequirePasswordChange = false;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.Update(user.Id, user);

            await _refreshTokenRepository.DeleteAllByUserId(user.Id);

            long currentUnixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            string revokeRedisKey = $"revoke_user_{user.Id}";
            await db.StringSetAsync(revokeRedisKey, currentUnixTimestamp, TimeSpan.FromMinutes(60));

            await db.KeyDeleteAsync(redisKey);

            return true;
        }

        public async Task<UserResponseDTO?> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetById(userId);

            if (user == null || user.Status != "Active")
                return null;

            return _mapper.Map<UserResponseDTO>(user);
        }

        public async Task<LoginResponseDTO?> UpdateProfileAsync(int userId, UpdateProfileDTO dto)
        {
            var user = await _userRepository.GetById(userId);

            if (user == null || user.Status != "Active")
                return null;

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


        private async Task<LoginResponseDTO?> ProcessSocialLogin(string email, string name, string? avatarUrl, string provider)
        {
            var user = await _userRepository.GetByEmail(email);

            if (user == null)
            {
                user = CreateSocialUser(email, name, avatarUrl, provider);
                var defaultRole = await _roleRepository.GetByName("Customer");
                if (defaultRole != null) user.Roles.Add(defaultRole);

                await _userRepository.Add(user);
            }
            else
            {
                if (user.Provider != provider || user.Status != "Active")
                    return null;

                if (user.AvatarUrl != avatarUrl && !string.IsNullOrEmpty(avatarUrl))
                {
                    user.AvatarUrl = avatarUrl;
                    await _userRepository.Update(user.Id, user);
                }
            }

            return await GenerateLoginResponse(user);
        }

        private User CreateSocialUser(string email, string name, string? avatarUrl, string provider)
        {
            return new User
            {
                Email = email,
                FullName = name ?? $"{provider} User",
                AvatarUrl = avatarUrl,
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

        private string GenerateSecureRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
