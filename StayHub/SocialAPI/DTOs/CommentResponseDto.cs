using System;

namespace SocialAPI.DTOs;

public class CommentResponseDto
{
    public int Id { get; set; }
    public int MomentId { get; set; }
    public int UserId { get; set; }
    public string Comment { get; set; } = null!;
    public DateTime? Timestamp { get; set; }

    // ✅ THÊM: thông tin người bình luận (được enrich ở MomentService)
    public string? UserName { get; set; }
    public string? AvatarUrl { get; set; }
}
