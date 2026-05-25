using AuthAPI.Models;

namespace AuthAPI.Helpers
{
    public interface IPasswordHelper
    {
        string Hash(User user, string password);
        bool Verify(User user, string hashedPassword, string providedPassword);
    }
}
