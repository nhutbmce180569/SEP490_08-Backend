using System.Collections.Generic;
using System.Threading.Tasks;
using BookingAPI.DTOs;

namespace BookingAPI.Services
{
    public interface IEligibleScheduleService
    {
        Task<List<EligibleScheduleDto>> GetEligibleSchedulesAsync(int userId);
    }
}
