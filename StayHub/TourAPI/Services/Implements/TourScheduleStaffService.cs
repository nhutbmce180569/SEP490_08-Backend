using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class TourScheduleStaffService : ITourScheduleStaffService
    {
        private readonly ITourScheduleStaffRepository _staffRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TourScheduleStaffService> _logger;

        public TourScheduleStaffService(ITourScheduleStaffRepository staffRepository, IHttpClientFactory httpClientFactory, ILogger<TourScheduleStaffService> logger)
        {
            _staffRepository = staffRepository;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task AssignStaffToScheduleAsync(AssignStaffRequestDto dto)
        {
            var isAssigned = await _staffRepository.IsStaffAssignedAsync(dto.ScheduleId, dto.StaffId);
            
            if (isAssigned)
            {
                throw new InvalidOperationException("This staff member has already been assigned to this schedule.");
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

            // After successfully assigning staff, add member to chat room asynchronously
            // This call should not crash the main flow if it fails
            try
            {
                await AddMemberToChatRoomAsync(dto.ScheduleId, dto.StaffId);
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - the staff assignment was already completed successfully
                _logger.LogError(ex, $"Failed to add staff {dto.StaffId} to chat room for schedule {dto.ScheduleId}. Error: {ex.Message}");
            }
        }

        private async Task AddMemberToChatRoomAsync(int scheduleId, int userId)
        {
            try
            {
                var addMemberRequest = new
                {
                    userId = userId
                };

                using var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync(
                    $"https://localhost:7010/api/chat/rooms/schedule/{scheduleId}/add-member",
                    addMemberRequest
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to add staff {userId} to chat room for schedule {scheduleId}. Status: {response.StatusCode}, Content: {errorContent}");
                }
                else
                {
                    _logger.LogInformation($"Staff {userId} added to chat room successfully for schedule {scheduleId}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP error when adding staff {userId} to chat room for schedule {scheduleId}: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, $"Timeout when adding staff {userId} to chat room for schedule {scheduleId}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error when adding staff {userId} to chat room for schedule {scheduleId}: {ex.Message}");
            }
        }

        public async Task RemoveStaffFromScheduleAsync(int scheduleId, int staffId)
        {
            // Get the assigned staff record
            var assignedStaff = await _staffRepository.GetAssignedStaffAsync(scheduleId, staffId);

            if (assignedStaff == null)
            {
                throw new KeyNotFoundException("Staff assignment not found for this schedule.");
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
                    detail.FullName = string.IsNullOrWhiteSpace(userInfo.FullName) ? $"Staff {staff.StaffId}" : userInfo.FullName;
                    detail.AvatarUrl = userInfo.AvatarUrl ?? string.Empty;
                }
                else
                {
                    detail.FullName = $"Staff {staff.StaffId}";
                    detail.AvatarUrl = string.Empty;
                }

                result.Add(detail);
            }

            return result;
        }

        public async Task<PaginationDTO<AssignedTourScheduleDto>> GetAssignedSchedulesAsync(
     int staffId, int page, int pageSize, bool upcomingOnly, string? tourName = null)
        {
            var (items, total) = await _staffRepository.GetAssignedSchedulesAsync(
                staffId, page, pageSize, upcomingOnly, tourName);

            var data = items.Select(assignment => new AssignedTourScheduleDto
            {
                ScheduleId = assignment.ScheduleId,
                TourId = assignment.Schedule?.TourId ?? 0,
                DepartureDate = assignment.Schedule?.DepartureDate ?? default,
                ReturnDate = assignment.Schedule?.ReturnDate ?? default,
                TourName = assignment.Schedule?.Tour?.Name ?? string.Empty,
                TourImageUrl = assignment.Schedule?.Tour?.ImageUrl ?? string.Empty,
                AssignedRole = assignment.AssignedRole ?? string.Empty
            }).ToList();

            return new PaginationDTO<AssignedTourScheduleDto>
            {
                Data = data,
                CurrentPage = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
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
