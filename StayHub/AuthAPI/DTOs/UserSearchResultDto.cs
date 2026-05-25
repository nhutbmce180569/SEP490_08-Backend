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
    }
}