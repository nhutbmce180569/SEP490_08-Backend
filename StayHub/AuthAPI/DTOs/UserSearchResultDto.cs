using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AuthAPI.DTOs
{
    public class UserSearchResultDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        public string Status { get; set; } = string.Empty;
        
        public string? AvatarUrl { get; set; }

        public List<int> RoleIds { get; set; } = new List<int>();
        public List<string> RoleNames { get; set; } = new List<string>();

        public string? PhoneNumber { get; set; }
    }
}