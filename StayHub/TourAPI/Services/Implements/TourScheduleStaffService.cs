using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<TourScheduleStaffService> _logger;
        private readonly INotificationInternalService _notificationService;
        private readonly ITourScheduleRepository _scheduleRepository;

        public TourScheduleStaffService(
            ITourScheduleStaffRepository staffRepository,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            ILogger<TourScheduleStaffService> logger,
            INotificationInternalService notificationService,
            ITourScheduleRepository scheduleRepository)
        {
            _staffRepository = staffRepository;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _notificationService = notificationService;
            _scheduleRepository = scheduleRepository;
        }

        public async Task AssignStaffToScheduleAsync(AssignStaffRequestDto dto)
        {
            var isAssigned = await _staffRepository.IsStaffAssignedAsync(dto.ScheduleId, dto.StaffId);

            if (isAssigned)
            {
                throw new InvalidOperationException("This staff member has already been assigned to this schedule.");
            }

            var newSchedule = await _scheduleRepository.GetByIdAsync(dto.ScheduleId);
            if (newSchedule == null) 
            {
                throw new KeyNotFoundException("Schedule not found.");
            }

            var assignedData = await _staffRepository.GetAssignedSchedulesAsync(dto.StaffId, 1, 1000, false, null);

            foreach (var assignment in assignedData.Items)
            {
                if (assignment.ScheduleId == dto.ScheduleId) continue;
                
                var existingSchedule = assignment.Schedule;
                if (existingSchedule != null)
                {
                    if (newSchedule.DepartureDate <= existingSchedule.ReturnDate && 
                        newSchedule.ReturnDate >= existingSchedule.DepartureDate)
                    {
                        throw new InvalidOperationException("This Staff member is already assigned to another tour schedule during the selected period.");
                    }
                }
            }

            var staffAssignment = new TourScheduleStaff
            {
                ScheduleId = dto.ScheduleId,
                StaffId = dto.StaffId,
                AssignedRole = dto.AssignedRole
            };

            await _staffRepository.AssignStaffAsync(staffAssignment);

            // ✨ LUỒNG TỰ ĐỘNG ADD STAFF VÀO GROUP CHAT
            try
            {
                await AddMemberToChatRoomAsync(dto.ScheduleId, dto.StaffId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to add staff {dto.StaffId} to chat room for schedule {dto.ScheduleId}. Error: {ex.Message}");
            }

            // ✨ GỬI THÔNG BÁO CHO STAFF
            try
            {
                var schedule = await _scheduleRepository.GetByIdAsync(dto.ScheduleId);
                string title = "New Tour Assignment";
                string role = dto.AssignedRole ?? "Staff";
                string tourName = schedule?.Tour?.Name ?? "Unknown Tour";
                string content = schedule != null 
                    ? $"You have been assigned to the tour '{tourName}' (Schedule ID: {schedule.Id}) with the role of {role}. Departure Date: {schedule.DepartureDate:MM/dd/yyyy}."
                    : $"You have been assigned to a tour schedule (ID: {dto.ScheduleId}) with the role of {role}.";
                    
                await _notificationService.NotifyUserAsync(dto.StaffId, title, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to staff {dto.StaffId} for schedule {dto.ScheduleId}. Error: {ex.Message}");
            }
        }

        private async Task AddMemberToChatRoomAsync(int scheduleId, int userId)
        {
            try
            {
                var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrWhiteSpace(token)) return;

                var addMemberRequest = new
                {
                    userIds = new List<int> { userId } // Khớp DTO đầu nhận của SocialAPI
                };

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                using var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("Authorization", token);

                var response = await client.PostAsJsonAsync(
                    $"https://localhost:7010/api/chat/rooms/schedule/{scheduleId}/members",
                    addMemberRequest
                );

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Staff {userId} automatically added to chat room for schedule {scheduleId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when sending HttpClient request to add staff to chat room: {ex.Message}");
            }
        }
        public async Task RemoveStaffFromScheduleAsync(int scheduleId, int staffId)
        {
            var assignedStaff = await _staffRepository.GetAssignedStaffAsync(scheduleId, staffId);

            if (assignedStaff == null)
            {
                throw new KeyNotFoundException("Staff assignment not found for this schedule.");
            }

            await _staffRepository.RemoveStaffAsync(assignedStaff);

            // ✨ LUỒNG TỰ ĐỘNG REMOVE STAFF KHỎI GROUP CHAT
            try
            {
                await RemoveMemberFromChatRoomAsync(scheduleId, staffId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to remove staff {staffId} from chat room for schedule {scheduleId}. Error: {ex.Message}");
            }
        }

        private async Task RemoveMemberFromChatRoomAsync(int scheduleId, int userId)
        {
            try
            {
                var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrWhiteSpace(token)) return;

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                using var client = new HttpClient(handler);
                client.DefaultRequestHeaders.Add("Authorization", token);

                var response = await client.DeleteAsync(
                    $"https://localhost:7010/api/chat/rooms/schedule/{scheduleId}/members/{userId}"
                );

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Staff {userId} automatically removed from chat room for schedule {scheduleId}");
                }
                else
                {
                    _logger.LogWarning($"Failed to automatically remove staff {userId} from chat room. Code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when sending HttpClient request to remove staff from chat room: {ex.Message}");
            }
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
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
                using var client = new HttpClient(handler);
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