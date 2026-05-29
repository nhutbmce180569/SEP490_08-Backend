using System;

namespace SocialAPI.Models;

public class ChatGroupInvitation
{
    public int Id { get; set; }
    public int ChatRoomId { get; set; }
    public int InviterId { get; set; }
    public int InviteeId { get; set; } 
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ChatRoom ChatRoom { get; set; } = null!;
}