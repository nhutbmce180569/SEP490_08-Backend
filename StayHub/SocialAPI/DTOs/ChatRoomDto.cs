using System;

namespace SocialAPI.DTOs
{
    public class ChatRoomDto
    {
        public int Id { get; set; }
        public string? RoomName { get; set; }
        public string? AvatarUrl { get; set; }
        public bool? IsGroupChat { get; set; }
        public int? ScheduleId { get; set; }
        public string? LatestMessage { get; set; }
        public DateTime? LatestMessageTime { get; set; }
        public bool? IsPinned { get; set; }
        public bool? IsMuted { get; set; }
    }
}
