namespace SocialAPI.Services
{
    public interface IBookingApiClient
    {
        Task<List<int>> GetCustomerIdsByScheduleAsync(int scheduleId);
        Task<bool> CheckCompletedBookingAsync(int customerId, List<int> scheduleIds);
        Task<List<int>> GetEligibleScheduleIdsByUserAsync(int userId);
    }
}
