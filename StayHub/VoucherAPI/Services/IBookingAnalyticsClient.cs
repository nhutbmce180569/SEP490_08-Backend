using VoucherAPI.DTOs;

namespace VoucherAPI.Services;

public interface IBookingAnalyticsClient
{
    Task<List<BookingTopCustomer>> GetTopCustomersAsync(int top, DateTime? from, DateTime? to);
}
