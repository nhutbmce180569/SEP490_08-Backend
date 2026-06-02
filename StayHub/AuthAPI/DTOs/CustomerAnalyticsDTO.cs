namespace AuthAPI.DTOs
{
    public class CustomerDemographicsDTO
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }
        public int NewCustomersInPeriod { get; set; }
        public List<LabelCountDTO> ByGender { get; set; } = [];
        public List<LabelCountDTO> ByProvider { get; set; } = [];
        public List<LabelCountDTO> ByStatus { get; set; } = [];
        public List<LabelCountDTO> ByAgeGroup { get; set; } = [];
        public List<TimeSeriesPointDTO> RegistrationTrend { get; set; } = [];
    }

    public class LabelCountDTO
    {
        public string Label { get; set; } = null!;
        public int Count { get; set; }
        public decimal Percentage { get; set; }
    }

    public class TimeSeriesPointDTO
    {
        public string Period { get; set; } = null!;
        public int Count { get; set; }
        public long? Amount { get; set; }
    }

    public class CustomerSummaryDTO
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

    public class CustomerListAnalyticsDTO
    {
        public List<CustomerSummaryDTO> Customers { get; set; } = [];
        public int Total { get; set; }
    }
}
