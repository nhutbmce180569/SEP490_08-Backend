namespace BookingAPI.DTOs
{
    /// <summary>Minimal booking ops for platform health (order analytics: customer-analytics).</summary>
    public class PlatformOperationsStatsDTO
    {
        public decimal CheckInRate { get; set; }
        public int PendingCancellationRequests { get; set; }
    }
}
