namespace AuthAPI.DTOs
{
    public class SocialAuthResultDTO
    {
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? AvatarUrl { get; set; }
    }
}
