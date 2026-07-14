namespace SocialAPI.Services
{
    public interface IBookingApiClient
    {
        Task<List<int>> GetCustomerIdsByScheduleAsync(int scheduleId);
    }
}
