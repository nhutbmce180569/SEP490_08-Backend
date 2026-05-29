using System;
using System.Collections.Generic;

namespace SocialAPI.Models;

public partial class ChatRoom
{
    public int Id { get; set; }

    public int? ScheduleId { get; set; }

    public string? RoomName { get; set; }

    public bool? IsGroupChat { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ChatMember> ChatMembers { get; set; } = new List<ChatMember>();

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
}
