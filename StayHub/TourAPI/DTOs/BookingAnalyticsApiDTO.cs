namespace TourAPI.DTOs
{
    public class BookingOverviewResponse
    {
        public string? Message { get; set; }
        public BookingOverviewData? Data { get; set; }
    }

    public class BookingOverviewData
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

    public class BookingSegmentsResponse
    {
        public string? Message { get; set; }
        public BookingSegmentsData? Data { get; set; }
    }

    public class BookingSegmentsData
    {
        public int NeverPurchased { get; set; }
        public int OneTimeBuyers { get; set; }
        public int RepeatBuyers { get; set; }
        public int NewBuyersInPeriod { get; set; }
        public int AtRiskCustomers { get; set; }
        public int HighValueCustomers { get; set; }
        public decimal BuyerConversionRate { get; set; }
    }

    public class BookingTrendsResponse
    {
        public string? Message { get; set; }
        public List<BookingTrendPoint>? Data { get; set; }
    }

    public class BookingTrendPoint
    {
        public string Period { get; set; } = null!;
        public int OrderCount { get; set; }
        public long Revenue { get; set; }
        public int UniqueCustomers { get; set; }
    }

    public class BookingTopCustomersResponse
    {
        public string? Message { get; set; }
        public List<BookingTopCustomer>? Data { get; set; }
    }

    public class BookingTopCustomer
    {
        public int CustomerId { get; set; }
        public long TotalSpend { get; set; }
        public int OrderCount { get; set; }
        public DateTime? LastOrderAt { get; set; }
        public int TotalTickets { get; set; }
    }

    public class BookingCustomerMetricsResponse
    {
        public string? Message { get; set; }
        public List<BookingCustomerMetrics>? Data { get; set; }
    }

    public class BookingCustomerMetricsDataResponse
    {
        public string? Message { get; set; }
        public BookingCustomerMetrics? Data { get; set; }
    }

    public class BookingCustomerMetrics
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
}
