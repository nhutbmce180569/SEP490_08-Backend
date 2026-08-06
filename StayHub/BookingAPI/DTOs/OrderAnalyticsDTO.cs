namespace BookingAPI.DTOs
{
    public class BookingStatisticsRequestDTO
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string GroupBy { get; set; } = "Day";
    }

    public class BookingStatisticsMetricsDTO
    {
        public long TotalRevenue { get; set; }
        public long TotalDiscount { get; set; }
        public long TotalPromotionDiscount { get; set; }
        public long TotalRefundAmount { get; set; }
        public long GrossRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int PaidOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int TotalTicketsSold { get; set; }
        public int NewCustomers { get; set; }
        public int RepeatCustomers { get; set; }
    }

    public class RevenueTrendPointDTO
    {
        public string Period { get; set; } = null!;
        public long Revenue { get; set; }
        public int OrderCount { get; set; }
    }

    public class TicketTypeSalesDTO
    {
        public int TicketTypeId { get; set; }
        public string? TicketTypeName { get; set; }
        public int QuantitySold { get; set; }
        public long Revenue { get; set; }
    }

    public class OrdersByHourDTO
    {
        public int Hour { get; set; }
        public int OrderCount { get; set; }
    }

    public class CheckInStatusRatioDTO
    {
        public string Status { get; set; } = null!;
        public int TicketCount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class CancellationReasonStatsDTO
    {
        public string Reason { get; set; } = null!;
        public int Count { get; set; }
    }

    public class DiscountBreakdownDTO
    {
        public string Type { get; set; } = null!;
        public long TotalAmount { get; set; }
        public int OrderCount { get; set; }
    }

    public class EventSalesDTO
    {
        public int ScheduleId { get; set; }
        public string? TourName { get; set; }
        public int TotalBookings { get; set; }
        public int TotalTickets { get; set; }
        public long TotalRevenue { get; set; }
    }

    public class BookingStatisticsResponseDTO
    {
        public BookingStatisticsMetricsDTO Metrics { get; set; } = new();
        public List<RevenueTrendPointDTO> RevenueTrend { get; set; } = [];
        public List<TicketTypeSalesDTO> SalesByTicketType { get; set; } = [];
        public List<OrdersByHourDTO> OrdersByHour { get; set; } = [];
        public List<CheckInStatusRatioDTO> CheckInRatio { get; set; } = [];
        public List<CancellationReasonStatsDTO> TopCancellationReasons { get; set; } = [];
        public List<DiscountBreakdownDTO> DiscountBreakdown { get; set; } = [];
        public List<EventSalesDTO> SalesByEvent { get; set; } = [];
    }

    public class OrderAnalyticsOverviewDTO
    {
        public int TotalOrders { get; set; }
        public int PaidOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CancelledOrders { get; set; }
        public long TotalRevenue { get; set; }
        public long AverageOrderValue { get; set; }
        public int UniqueCustomersWithOrders { get; set; }
        public int RepeatCustomers { get; set; }
        public decimal RepeatCustomerRate { get; set; }
        public int TotalTicketsSold { get; set; }
        public long TotalDiscountApplied { get; set; }
        public int OrdersWithVoucher { get; set; }
        public int PendingCancellationRequests { get; set; }
        public int ApprovedCancellationRequests { get; set; }
        public long TotalRefundAmount { get; set; }
        public decimal CancellationRate { get; set; }
    }

    public class CustomerOrderSegmentDTO
    {
        public int NeverPurchased { get; set; }
        public int OneTimeBuyers { get; set; }
        public int RepeatBuyers { get; set; }
        public int NewBuyersInPeriod { get; set; }
        public int AtRiskCustomers { get; set; }
        public int HighValueCustomers { get; set; }
        public decimal BuyerConversionRate { get; set; }
    }

    public class CustomerOrderMetricsDTO
    {
        public int CustomerId { get; set; }
        public int TotalOrders { get; set; }
        public int PaidOrders { get; set; }
        public long TotalSpend { get; set; }
        public long AverageOrderValue { get; set; }
        public DateTime? FirstOrderAt { get; set; }
        public DateTime? LastOrderAt { get; set; }
        public int TotalTickets { get; set; }
        public int CancelledOrders { get; set; }
        public int PendingOrders { get; set; }
    }

    public class TopCustomerOrderDTO
    {
        public int CustomerId { get; set; }
        public long TotalSpend { get; set; }
        public int OrderCount { get; set; }
        public DateTime? LastOrderAt { get; set; }
        public int TotalTickets { get; set; }
    }

    public class OrderTrendPointDTO
    {
        public string Period { get; set; } = null!;
        public int OrderCount { get; set; }
        public long Revenue { get; set; }
        public int UniqueCustomers { get; set; }
    }

    public class OrderAnalyticsBundleDTO
    {
        public OrderAnalyticsOverviewDTO Overview { get; set; } = new();
        public CustomerOrderSegmentDTO Segments { get; set; } = new();
        public List<OrderTrendPointDTO> Trends { get; set; } = [];
        public List<TopCustomerOrderDTO> TopCustomers { get; set; } = [];
        public List<CustomerOrderMetricsDTO> CustomerMetrics { get; set; } = [];
    }
}
