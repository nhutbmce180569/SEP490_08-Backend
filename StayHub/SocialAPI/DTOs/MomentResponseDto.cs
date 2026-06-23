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

    // ✅ Số lượt thích. Tên field PHẢI khớp với Flutter (reactionCount).
    //    Giữ TotalLikes để không phá vỡ code/web cũ; thêm ReactionCount cho app.
    public int TotalLikes { get; set; }
    public int ReactionCount { get; set; }

    // ✅ THÊM: trạng thái đã-thích-bởi-người-đang-đăng-nhập (set trong MomentService)
    public bool IsLikedByMe { get; set; }

    public List<CommentResponseDto> Comments { get; set; } = new();

    public MomentUserDto User { get; set; } = new MomentUserDto();
    public List<ReactionRequestDto> MomentReactions { get; set; } = new();
}
