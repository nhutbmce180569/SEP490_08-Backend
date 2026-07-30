using AuthAPI.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthAPI.Helpers.Implements
{
    public class JwtHelper : IJwtHelper
    {
        private readonly IConfiguration _configuration;

        public JwtHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            // 1. Khởi tạo danh sách Claims cơ bản
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("FullName", user.FullName ?? string.Empty),
                new Claim("Provider", user.Provider ?? "Local"),
                
                // THÊM MỚI: Đẩy AvatarUrl lên Claim (Xử lý null an toàn)
                new Claim("AvatarUrl", user.AvatarUrl ?? string.Empty),
                new Claim("RequirePasswordChange", user.RequirePasswordChange.ToString().ToLowerInvariant()),

                new Claim("SecurityStamp", user.SecurityStamp ?? Guid.NewGuid().ToString())
            };

            // 2. Thêm Claims cho Roles bằng LINQ
            if (user.Roles != null && user.Roles.Any())
            {
                claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role.Name)));
            }

            // 3. Kiểm tra tính hợp lệ của JWT Key
            var keyString = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(keyString) || keyString.Length < 32)
            {
                throw new InvalidOperationException("JWT Key is missing or too short. It must be at least 32 characters long.");
            }

            // 4. Xử lý an toàn cho thời gian hết hạn
            if (!double.TryParse(_configuration["Jwt:ExpireMinutes"], out double expireMinutes))
            {
                expireMinutes = 15;
            }

            // 5. Cấu hình chữ ký và tạo Token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                NotBefore = now,
                Expires = now.AddMinutes(expireMinutes),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}
