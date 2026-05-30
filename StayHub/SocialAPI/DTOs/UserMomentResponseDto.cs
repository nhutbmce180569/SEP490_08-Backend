using System;

namespace SocialAPI.DTOs
{
    public class UserMomentResponseDto
    {
        public int Id { get; set; }
        public int ScheduleId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public string Privacy { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
    }
}
