using System;

namespace SocialAPI.DTOs
{
    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int ChatRoomId { get; set; }
        public int SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? SenderAvatar { get; set; }
        public string Content { get; set; } = null!;
        public bool? IsRead { get; set; }
        public DateTime? SentAt { get; set; }
    }
}
