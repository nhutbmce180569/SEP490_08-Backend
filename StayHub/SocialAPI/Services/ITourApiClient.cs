using System.Collections.Generic;
using System.Threading.Tasks;
using SocialAPI.DTOs;

namespace SocialAPI.Services
{
    public interface ITourApiClient
    {
        Task<TourScheduleMetadataDto?> GetScheduleMetadataAsync(int scheduleId, string? bearerToken);
        Task<bool> VerifyManagerAsync(int scheduleId, int managerId);
        Task<List<int>> GetStaffIdsByScheduleAsync(int scheduleId);
        Task<List<int>> GetStaffScheduleIdsAsync(int staffId);
    }
}
