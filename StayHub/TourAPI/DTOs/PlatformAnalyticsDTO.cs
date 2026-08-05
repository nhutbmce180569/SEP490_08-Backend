using System.ComponentModel.DataAnnotations;

namespace TourAPI.DTOs
{
    public class PlatformAnalyticsQueryDTO
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        [Range(1, 20, ErrorMessage = "Top must be between 1 and 20.")]
        public int Top { get; set; } = 10;
    }

    /// <summary>High-level platform pulse — no order/review/customer-behavior detail (see customer-analytics).</summary>
    public class PlatformAnalyticsOverviewDTO
    {
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int NewUsersInPeriod { get; set; }
        public int TotalManagers { get; set; }
        public int TotalStaff { get; set; }
        public int TotalTours { get; set; }
        public int ActiveTours { get; set; }
        public int TotalSchedules { get; set; }
        public int UpcomingSchedules { get; set; }
        public decimal ScheduleOccupancyRate { get; set; }
        public int ActiveVouchers { get; set; }
        public int TotalTourMoments { get; set; }
        public int TotalChatRooms { get; set; }
        public decimal CheckInRate { get; set; }
        public int PendingCancellationRequests { get; set; }
    }

    /// <summary>All-user ecosystem by role — not customer demographics (see customer-analytics/demographics).</summary>
    public class PlatformUserAnalyticsDTO
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
        public List<AnalyticsLabelCountDTO> ByRole { get; set; } = [];
    }

    public class PlatformCatalogAnalyticsDTO
    {
        public int TotalTours { get; set; }
        public int ActiveTours { get; set; }
        public int InactiveTours { get; set; }
        public int TotalSchedules { get; set; }
        public int UpcomingSchedules { get; set; }
        public int OngoingSchedules { get; set; }
        public int CompletedSchedules { get; set; }
        public int TotalTicketCapacity { get; set; }
        public int TotalTicketsSold { get; set; }
        public int TotalTicketsAvailable { get; set; }
        public decimal ScheduleOccupancyRate { get; set; }
        public List<AnalyticsLabelCountDTO> ToursByCategory { get; set; } = [];
        public List<AnalyticsLabelCountDTO> ToursByCity { get; set; } = [];
        public List<AnalyticsLabelCountDTO> ToursByStatus { get; set; } = [];
        public List<TopTourEngagementDTO> TopBookedTours { get; set; } = [];
    }

    public class PlatformVoucherAnalyticsDTO
    {
        public int TotalVouchers { get; set; }
        public int ActiveVouchers { get; set; }
        public int ExpiredVouchers { get; set; }
        public int TotalRedemptions { get; set; }
        public decimal RedemptionRate { get; set; }
        public List<AnalyticsLabelCountDTO> ByDiscountType { get; set; } = [];
        public List<AnalyticsLabelCountDTO> ByUserVoucherStatus { get; set; } = [];
    }

    public class PlatformSocialAnalyticsDTO
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
        public List<AnalyticsLabelCountDTO> MomentsByPrivacy { get; set; } = [];
        public List<AnalyticsLabelCountDTO> FriendshipStatusDistribution { get; set; } = [];
    }

    /// <summary>Platform operations health — not order/customer analytics (see customer-analytics).</summary>
    public class PlatformHealthAnalyticsDTO
    {
        public decimal ScheduleOccupancyRate { get; set; }
        public decimal CheckInRate { get; set; }
        public decimal ReviewResponseRate { get; set; }
        public decimal VoucherRedemptionRate { get; set; }
        public int PendingCancellationRequests { get; set; }
        public string OverallStatus { get; set; } = "Healthy";
        public List<PlatformHealthIndicatorDTO> Indicators { get; set; } = [];
    }

    public class PlatformHealthIndicatorDTO
    {
        public string Name { get; set; } = null!;
        public decimal Value { get; set; }
        public string Unit { get; set; } = "%";
        public string Status { get; set; } = "Good";
        public string? Description { get; set; }
    }

    public class PlatformVoucherStatsDTO
    {
        public int TotalVouchers { get; set; }
        public int ActiveVouchers { get; set; }
        public int InactiveVouchers { get; set; }
        public int ExpiredVouchers { get; set; }
        public int TotalRedemptions { get; set; }
        public int TotalUserVoucherAssignments { get; set; }
        public int UsedUserVouchers { get; set; }
        public int AvailableUserVouchers { get; set; }
        public int ExpiredUserVouchers { get; set; }
        public decimal RedemptionRate { get; set; }
        public List<LabelCountDTO> ByDiscountType { get; set; } = [];
        public List<LabelCountDTO> ByUserVoucherStatus { get; set; } = [];
    }

    public class LabelCountDTO
    {
        public string Label { get; set; } = null!;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }
}
