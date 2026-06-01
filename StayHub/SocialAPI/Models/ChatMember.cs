using System;
using System.Collections.Generic;

namespace SocialAPI.Models;

public partial class ChatMember
{
    public int Id { get; set; }

    public int ChatRoomId { get; set; }

    public int UserId { get; set; }

    public DateTime? JoinedAt { get; set; }

    public virtual ChatRoom ChatRoom { get; set; } = null!;
    public bool? IsPinned { get; set; }
    public bool? IsMuted { get; set; }
}
