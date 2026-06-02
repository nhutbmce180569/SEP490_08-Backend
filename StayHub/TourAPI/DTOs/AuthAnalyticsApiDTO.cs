namespace TourAPI.DTOs
{
    public class AuthDemographicsResponse
    {
        public string? Message { get; set; }
        public AuthDemographicsData? Data { get; set; }
    }

    public class AuthDemographicsData
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }
        public int NewCustomersInPeriod { get; set; }
        public List<AnalyticsLabelCountDTO>? ByGender { get; set; }
        public List<AnalyticsLabelCountDTO>? ByProvider { get; set; }
        public List<AnalyticsLabelCountDTO>? ByStatus { get; set; }
        public List<AnalyticsLabelCountDTO>? ByAgeGroup { get; set; }
        public List<CustomerTrendPointDTO>? RegistrationTrend { get; set; }
    }

    public class AuthCustomerListResponse
    {
        public string? Message { get; set; }
        public AuthCustomerListData? Data { get; set; }
    }

    public class AuthCustomerListData
    {
        public int Total { get; set; }
        public List<AuthCustomerSummary>? Customers { get; set; }
    }

    public class AuthCustomerSummary
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? Provider { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Status { get; set; }
        public DateTime? LastOnline { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class AuthCustomerBatchResponse
    {
        public string? Message { get; set; }
        public List<AuthCustomerBatchItem>? Data { get; set; }
    }

    public class AuthCustomerBatchItem
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
    }

    public class AuthCustomerDetailResponse
    {
        public string? Message { get; set; }
        public AuthCustomerSummary? Data { get; set; }
    }
}
