namespace SocialAPI.DTOs;

public class PlatformSocialStatsDTO
{
    public int TotalFriendships { get; set; }
    public int AcceptedFriendships { get; set; }
    public int PendingFriendRequests { get; set; }
    public int DeclinedFriendRequests { get; set; }
    public int TotalChatRooms { get; set; }
    public int GroupChatRooms { get; set; }
    public int PrivateChatRooms { get; set; }
    public int TotalChatMessages { get; set; }
    public int UnreadChatMessages { get; set; }
    public int TotalTourMoments { get; set; }
    public int TotalMomentReactions { get; set; }
    public int TotalMomentComments { get; set; }
    public int TotalLocationLogs { get; set; }
    public List<LabelCountDTO> MomentsByPrivacy { get; set; } = [];
    public List<LabelCountDTO> FriendshipStatusDistribution { get; set; } = [];
}

public class LabelCountDTO
{
    public string Label { get; set; } = null!;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}
