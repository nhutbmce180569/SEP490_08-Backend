using System;
using System.Collections.Generic;

namespace SocialAPI.DTOs;

public class MomentResponseDto
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int UserId { get; set; }
    public string ImageUrl { get; set; } = null!;
    public string? Caption { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string Privacy { get; set; } = null!;

    // Khôi phục lại 2 trường bị thiếu
    public int TotalLikes { get; set; }
    public List<CommentResponseDto> Comments { get; set; } = new();

    // Các trường object chứa dữ liệu User và Reaction
    public MomentUserDto User { get; set; } = new MomentUserDto();
    public List<ReactionRequestDto> MomentReactions { get; set; } = new();
}