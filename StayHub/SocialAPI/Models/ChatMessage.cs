using System;
using System.Collections.Generic;

namespace SocialAPI.Models;

public partial class ChatMessage
{
    public int Id { get; set; }

    public int ChatRoomId { get; set; }

    public int SenderId { get; set; }

    public string Content { get; set; } = null!;

    public bool? IsRead { get; set; }

    public DateTime? SentAt { get; set; }

    public virtual ChatRoom ChatRoom { get; set; } = null!;
}
