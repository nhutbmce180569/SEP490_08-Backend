using AuthAPI.Models;

namespace AuthAPI.Helpers
{
    public interface IJwtHelper
    {
        string GenerateToken(User user);
    }
}
