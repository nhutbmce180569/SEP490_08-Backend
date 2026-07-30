namespace TourAPI.Services
{
    public interface IBookingApiClient
    {
        Task<bool> HasOrdersForScheduleAsync(int scheduleId);
    }
}
