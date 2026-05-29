namespace SocialAPI.DTOs;

public class ReactionRequestDto
{
    public int UserId { get; set; }
    public int MomentId { get; set; }
    public bool? IsLike { get; set; } = true;
}