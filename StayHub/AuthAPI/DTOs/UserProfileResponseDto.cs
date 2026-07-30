using System;

namespace AuthAPI.DTOs
{
    public class UserProfileResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
