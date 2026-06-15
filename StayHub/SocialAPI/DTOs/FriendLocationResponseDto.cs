using System;

namespace SocialAPI.DTOs
{
    public class FriendLocationResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class LiveScheduleMemberLocationDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
