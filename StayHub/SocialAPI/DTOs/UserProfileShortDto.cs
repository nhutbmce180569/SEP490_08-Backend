namespace SocialAPI.DTOs
{
    public class UserProfileShortDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }
}