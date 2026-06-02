using System.Net.Http.Json;
using System.Text.Json;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class TourScheduleStaffService : ITourScheduleStaffService
    {
        private readonly ITourScheduleStaffRepository _staffRepository;
        private readonly IHttpClientFactory _httpClientFactory;

        public TourScheduleStaffService(ITourScheduleStaffRepository staffRepository, IHttpClientFactory httpClientFactory)
        {
            _staffRepository = staffRepository;
            _httpClientFactory = httpClientFactory;
        }

        public async Task AssignStaffToScheduleAsync(AssignStaffRequestDto dto)
        {
            // Check if staff is already assigned to this schedule
            var isAssigned = await _staffRepository.IsStaffAssignedAsync(dto.ScheduleId, dto.StaffId);
            
            if (isAssigned)
            {
                throw new InvalidOperationException("Nhân viên này đã được phân công cho lịch trình này.");
            }

            // Create new TourScheduleStaff entity
            var staffAssignment = new TourScheduleStaff
            {
                ScheduleId = dto.ScheduleId,
                StaffId = dto.StaffId,
                AssignedRole = dto.AssignedRole
            };

            // Add to repository
            await _staffRepository.AssignStaffAsync(staffAssignment);
        }

        public async Task RemoveStaffFromScheduleAsync(int scheduleId, int staffId)
        {
            // Get the assigned staff record
            var assignedStaff = await _staffRepository.GetAssignedStaffAsync(scheduleId, staffId);

            if (assignedStaff == null)
            {
                throw new KeyNotFoundException("Không tìm thấy phân công nhân viên cho lịch trình này.");
            }

            // Remove from repository
            await _staffRepository.RemoveStaffAsync(assignedStaff);
        }

        public async Task<List<ScheduleStaffDetailDto>> GetStaffByScheduleIdAsync(int scheduleId)
        {
            var staffRecords = await _staffRepository.GetStaffByScheduleIdAsync(scheduleId);
            if (staffRecords == null || !staffRecords.Any())
            {
                return new List<ScheduleStaffDetailDto>();
            }

            var staffIds = staffRecords.Select(s => s.StaffId).Distinct().ToList();
            var userDict = new Dictionary<int, BatchUserResponseDto>();

            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync("https://localhost:7010/api/users/batch", staffIds);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                    try
                    {
                        var users = JsonSerializer.Deserialize<List<BatchUserResponseDto>>(responseString, options);
                        if (users != null) userDict = users.ToDictionary(u => u.Id, u => u);
                    }
                    catch
                    {
                        var wrapper = JsonSerializer.Deserialize<BatchUserApiResponse>(responseString, options);
                        if (wrapper?.Data != null) userDict = wrapper.Data.ToDictionary(u => u.Id, u => u);
                    }
                }
            }
            catch
            {
                // Fallback nếu kết nối lỗi
            }

            var result = new List<ScheduleStaffDetailDto>();
            foreach (var staff in staffRecords)
            {
                var detail = new ScheduleStaffDetailDto
                {
                    StaffId = staff.StaffId,
                    AssignedRole = staff.AssignedRole ?? string.Empty
                };

                if (userDict.TryGetValue(staff.StaffId, out var userInfo))
                {
                    detail.FullName = string.IsNullOrWhiteSpace(userInfo.FullName) ? $"Nhân viên {staff.StaffId}" : userInfo.FullName;
                    detail.AvatarUrl = userInfo.AvatarUrl ?? string.Empty;
                }
                else
                {
                    detail.FullName = $"Nhân viên {staff.StaffId}";
                    detail.AvatarUrl = string.Empty;
                }

                result.Add(detail);
            }

            return result;
        }

        public async Task<List<AssignedTourScheduleDto>> GetAssignedSchedulesAsync(int staffId)
        {
            var assignedSchedules = await _staffRepository.GetAssignedSchedulesAsync(staffId);
            if (assignedSchedules == null || !assignedSchedules.Any())
            {
                return new List<AssignedTourScheduleDto>();
            }

            return assignedSchedules.Select(assignment => new AssignedTourScheduleDto
            {
                ScheduleId = assignment.ScheduleId,
                TourId = assignment.Schedule?.TourId ?? 0,
                DepartureDate = assignment.Schedule?.DepartureDate ?? default,
                ReturnDate = assignment.Schedule?.ReturnDate ?? default,
                TourName = assignment.Schedule?.Tour?.Name ?? string.Empty,
                TourImageUrl = assignment.Schedule?.Tour?.ImageUrl ?? string.Empty,
                AssignedRole = assignment.AssignedRole ?? string.Empty
            }).ToList();
        }

        private class BatchUserResponseDto
        {
            public int Id { get; set; }
            public string? FullName { get; set; }
            public string? AvatarUrl { get; set; }
        }

        private class BatchUserApiResponse
        {
            public List<BatchUserResponseDto>? Data { get; set; }
        }
    }
}
