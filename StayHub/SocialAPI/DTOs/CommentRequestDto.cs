namespace SocialAPI.DTOs;

public class CommentRequestDto
{
    public int UserId { get; set; }
    public string Comment { get; set; } = null!;
}