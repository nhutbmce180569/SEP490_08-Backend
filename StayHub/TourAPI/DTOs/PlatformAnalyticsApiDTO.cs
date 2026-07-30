namespace TourAPI.DTOs
{
    public class PlatformUserStatsResponse
    {
        public string? Message { get; set; }
        public PlatformUserStatsData? Data { get; set; }
    }

    public class PlatformUserStatsData
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int NewUsersInPeriod { get; set; }
        public int OnlineRecently { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalManagers { get; set; }
        public int TotalStaff { get; set; }
        public int TotalAdmins { get; set; }
        public List<AnalyticsLabelCountDTO>? ByRole { get; set; }
    }

    public class PlatformOperationsStatsResponse
    {
        public string? Message { get; set; }
        public PlatformOperationsStatsData? Data { get; set; }
    }

    /// <summary>Internal ops subset for platform health (not full order analytics).</summary>
    public class PlatformOperationsStatsData
    {
        public decimal CheckInRate { get; set; }
        public int PendingCancellationRequests { get; set; }
    }

    public class PlatformVoucherStatsResponse
    {
        public string? Message { get; set; }
        public PlatformVoucherStatsData? Data { get; set; }
    }

    public class PlatformVoucherStatsData
    {
        public int TotalVouchers { get; set; }
        public int ActiveVouchers { get; set; }
        public int InactiveVouchers { get; set; }
        public int ExpiredVouchers { get; set; }
        public int TotalRedemptions { get; set; }
        public decimal RedemptionRate { get; set; }
        public List<AnalyticsLabelCountDTO>? ByDiscountType { get; set; }
        public List<AnalyticsLabelCountDTO>? ByUserVoucherStatus { get; set; }
    }

    public class PlatformSocialStatsResponse
    {
        public string? Message { get; set; }
        public PlatformSocialStatsData? Data { get; set; }
    }

    public class PlatformSocialStatsData
    {
        public int TotalFriendships { get; set; }
        public int AcceptedFriendships { get; set; }
        public int PendingFriendRequests { get; set; }
        public int TotalChatRooms { get; set; }
        public int GroupChatRooms { get; set; }
        public int TotalChatMessages { get; set; }
        public int UnreadChatMessages { get; set; }
        public int TotalTourMoments { get; set; }
        public int TotalMomentReactions { get; set; }
        public int TotalMomentComments { get; set; }
        public List<AnalyticsLabelCountDTO>? MomentsByPrivacy { get; set; }
        public List<AnalyticsLabelCountDTO>? FriendshipStatusDistribution { get; set; }
    }
}
