using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocialAPI.Integration
{
    public interface IAuthServiceClient
    {
        Task<Dictionary<int, UserDto>> GetUsersBatchAsync(List<int> userIds, string? token = null);
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}