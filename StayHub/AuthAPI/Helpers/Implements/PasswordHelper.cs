using AuthAPI.Models;
using Microsoft.AspNetCore.Identity;

namespace AuthAPI.Helpers.Implements
{
    public class PasswordHelper : IPasswordHelper
    {
        private readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();

        public string Hash(User user, string password) => _hasher.HashPassword(user, password);
        public bool Verify(User user, string hashedPassword, string providedPassword)
        {
            return _hasher.VerifyHashedPassword(user, hashedPassword, providedPassword) == PasswordVerificationResult.Success;
        }
    }
}
