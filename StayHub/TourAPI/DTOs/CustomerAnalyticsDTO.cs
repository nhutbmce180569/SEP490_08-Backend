using System.ComponentModel.DataAnnotations;

namespace TourAPI.DTOs
{
    public class CustomerAnalyticsQueryDTO
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        [RegularExpression("^(day|week|month)$", ErrorMessage = "Granularity must be day, week, or month.")]
        public string Granularity { get; set; } = "day";

        [Range(1, 1000, ErrorMessage = "Top must be between 1 and 1000.")]
        public int Top { get; set; } = 10;
    }

    public class CustomerAnalyticsOverviewDTO
    {
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int NewCustomersInPeriod { get; set; }
        public int TotalOrders { get; set; }
        public int PaidOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CancelledOrders { get; set; }
        public long TotalRevenue { get; set; }
        public long AverageOrderValue { get; set; }
        public int UniqueBuyers { get; set; }
        public int RepeatCustomers { get; set; }
        public decimal RepeatCustomerRate { get; set; }
        public decimal BuyerConversionRate { get; set; }
        public decimal CancellationRate { get; set; }
        public long TotalRefundAmount { get; set; }
        public int TotalReviews { get; set; }
        public int UniqueReviewers { get; set; }
        public decimal AverageReviewRating { get; set; }
        public int TotalWishlists { get; set; }
        public int UniqueWishlistCustomers { get; set; }
        public decimal ReviewParticipationRate { get; set; }
        public decimal WishlistToBuyerRate { get; set; }
    }

    public class CustomerDemographicsAnalyticsDTO
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }
        public int NewCustomersInPeriod { get; set; }
        public List<AnalyticsLabelCountDTO> ByGender { get; set; } = [];
        public List<AnalyticsLabelCountDTO> ByProvider { get; set; } = [];
        public List<AnalyticsLabelCountDTO> ByStatus { get; set; } = [];
        public List<AnalyticsLabelCountDTO> ByAgeGroup { get; set; } = [];
    }

    public class AnalyticsLabelCountDTO
    {
        public string Label { get; set; } = null!;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class CustomerSegmentAnalyticsDTO
    {
        public int NeverPurchased { get; set; }
        public int OneTimeBuyers { get; set; }
        public int RepeatBuyers { get; set; }
        public int NewBuyersInPeriod { get; set; }
        public int AtRiskCustomers { get; set; }
        public int HighValueCustomers { get; set; }
        public decimal BuyerConversionRate { get; set; }
        public List<AnalyticsLabelCountDTO> OrderStatusDistribution { get; set; } = [];
        public List<AnalyticsLabelCountDTO> RatingDistribution { get; set; } = [];
    }

    public class CustomerTrendAnalyticsDTO
    {
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public string Granularity { get; set; } = "day";
        public List<CustomerTrendPointDTO> RegistrationTrend { get; set; } = [];
        public List<CustomerTrendPointDTO> OrderTrend { get; set; } = [];
        public List<CustomerTrendPointDTO> RevenueTrend { get; set; } = [];
    }

    public class CustomerTrendPointDTO
    {
        public string Period { get; set; } = null!;
        public int Count { get; set; }
        public long? Amount { get; set; }
        public int? UniqueCustomers { get; set; }
    }

    public class TopCustomerAnalyticsDTO
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public long TotalSpend { get; set; }
        public int OrderCount { get; set; }
        public int TotalTickets { get; set; }
        public DateTime? LastOrderAt { get; set; }
        public int ReviewCount { get; set; }
        public int WishlistCount { get; set; }
        public decimal? AverageRatingGiven { get; set; }
    }

    public class CustomerEngagementAnalyticsDTO
    {
        public int TotalReviews { get; set; }
        public int VisibleReviews { get; set; }
        public int HiddenReviews { get; set; }
        public int UniqueReviewers { get; set; }
        public decimal AverageRating { get; set; }
        public List<AnalyticsLabelCountDTO> RatingDistribution { get; set; } = [];
        public int TotalWishlists { get; set; }
        public int UniqueWishlistCustomers { get; set; }
        public decimal AverageWishlistsPerCustomer { get; set; }
        public List<TopTourEngagementDTO> TopWishlistedTours { get; set; } = [];
        public List<TopTourEngagementDTO> TopReviewedTours { get; set; } = [];
        public decimal ReviewParticipationRate { get; set; }
    }

    public class TopTourEngagementDTO
    {
        public int TourId { get; set; }
        public string? TourName { get; set; }
        public int Count { get; set; }
        public decimal? AverageRating { get; set; }
    }

    public class CustomerListItemAnalyticsDTO
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? Status { get; set; }
        public string? Provider { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastOnline { get; set; }
        public int TotalOrders { get; set; }
        public long TotalSpend { get; set; }
        public int ReviewCount { get; set; }
        public int WishlistCount { get; set; }
        public DateTime? LastOrderAt { get; set; }
        public string CustomerSegment { get; set; } = "NeverPurchased";
    }

    public class CustomerDetailAnalyticsDTO
    {
        public int CustomerId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Provider { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastOnline { get; set; }
        public int TotalOrders { get; set; }
        public int PaidOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CancelledOrders { get; set; }
        public long TotalSpend { get; set; }
        public long AverageOrderValue { get; set; }
        public int TotalTickets { get; set; }
        public DateTime? FirstOrderAt { get; set; }
        public DateTime? LastOrderAt { get; set; }
        public int ReviewCount { get; set; }
        public decimal? AverageRatingGiven { get; set; }
        public int WishlistCount { get; set; }
        public string CustomerSegment { get; set; } = "NeverPurchased";
        public bool IsAtRisk { get; set; }
        public bool IsHighValue { get; set; }
    }

    public class CustomerListQueryDTO
    {
        public string? Search { get; set; }

        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        [RegularExpression("^(totalSpend|orderCount|reviewCount|wishlistCount|createdAt|lastOrderAt)$",
            ErrorMessage = "SortBy must be one of: totalSpend, orderCount, reviewCount, wishlistCount, createdAt, lastOrderAt.")]
        public string SortBy { get; set; } = "totalSpend";

        [RegularExpression("^(asc|desc)$", ErrorMessage = "SortOrder must be asc or desc.")]
        public string SortOrder { get; set; } = "desc";
    }
}
