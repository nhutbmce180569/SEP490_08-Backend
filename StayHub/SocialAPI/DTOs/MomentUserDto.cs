namespace SocialAPI.DTOs;

public class MomentUserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = "Anonymous user";
    public string? AvatarUrl { get; set; }
}