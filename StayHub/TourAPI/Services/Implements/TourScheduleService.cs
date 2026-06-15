using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Security.Claims; // 💡 ĐÃ THÊM: Để bóc Claims danh tính
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class TourScheduleService : ITourScheduleService
    {
        private readonly ITourScheduleRepository _scheduleRepo;
        private readonly ITourRepository _tourRepo;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBookingApiClient _bookingApiClient;
        private readonly ILogger<TourScheduleService> _logger;

        public TourScheduleService(
            ITourScheduleRepository scheduleRepo,
            IMapper mapper,
            ITourRepository tourRepo,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            IBookingApiClient bookingApiClient,
            ILogger<TourScheduleService> logger)
        {
            _scheduleRepo = scheduleRepo;
            _tourRepo = tourRepo;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _bookingApiClient = bookingApiClient;
            _logger = logger;
        }

        public async Task<PaginationDTO<ReadTourScheduleDTO>> GetAllSchedulesAsync(int page, int pageSize)
        {
            var schedules = await _scheduleRepo.GetAllAsync(page, pageSize);
            var total = await _scheduleRepo.CountAllAsync();

            return new PaginationDTO<ReadTourScheduleDTO>
            {
                Data = _mapper.Map<List<ReadTourScheduleDTO>>(schedules),
                CurrentPage = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public async Task<ReadTourScheduleDTO> GetScheduleByIdAsync(int id)
        {
            var schedule = await _scheduleRepo.GetByIdAsync(id);
            if (schedule == null) throw new Exception("Tour schedule not found.");

            return _mapper.Map<ReadTourScheduleDTO>(schedule);
        }

        public async Task<PaginationDTO<ReadTourScheduleDTO>> SearchSchedulesByTourNameAsync(string tourName, int page, int pageSize)
        {
            if (string.IsNullOrWhiteSpace(tourName))
                return await GetAllSchedulesAsync(page, pageSize);

            var schedules = await _scheduleRepo.SearchByTourNameAsync(tourName.Trim(), page, pageSize);
            var total = await _scheduleRepo.CountByTourNameAsync(tourName.Trim());

            var list = _mapper.Map<List<ReadTourScheduleDTO>>(schedules);

            return new PaginationDTO<ReadTourScheduleDTO>
            {
                Data = list,
                CurrentPage = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public async Task<ReadTourScheduleDTO> CreateScheduleAsync(CreateTourScheduleDTO dto)
        {
            if (dto.DepartureDate >= dto.ReturnDate)
            {
                throw new Exception("Departure date must be before the return date.");
            }

            await ValidateScheduleDuration(dto.TourId, dto.DepartureDate, dto.ReturnDate);

            var schedule = _mapper.Map<TourSchedule>(dto);
            await _scheduleRepo.AddAsync(schedule);

            var resultDto = _mapper.Map<ReadTourScheduleDTO>(schedule);

            // ✨ LUỒNG TỰ ĐỘNG TẠO PHÒNG CHAT & ADD MANAGER
            try
            {
                // 1. Chủ động kéo thông tin Tour để lấy tên thật
                var tourInfo = await _tourRepo.GetById(dto.TourId);
                var tourName = tourInfo?.Name ?? "Tour";

                // 2. Định dạng Tên đoạn chat = Tên Tour _ Tên/Mã Schedule (Sử dụng ngày khởi hành để phân biệt trên UI)
                string scheduleName = dto.DepartureDate.ToString("dd/MM/yyyy");
                string fullRoomName = $"{tourName} _{scheduleName}";

                await CreateChatRoomForScheduleAsync(schedule.Id, fullRoomName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create chat room for schedule {schedule.Id}. Error: {ex.Message}");
            }

            return resultDto;
        }

        private async Task CreateChatRoomForScheduleAsync(int scheduleId, string fullRoomName)
        {
            try
            {
                var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
                if (string.IsNullOrWhiteSpace(token)) return;

                // Bóc tách ID của Manager đang tạo Schedule từ JWT Token
                var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? _httpContextAccessor.HttpContext?.User.FindFirst("id")?.Value;

                var chatRoomRequest = new
                {
                    scheduleId = scheduleId,
                    roomName = fullRoomName // Định dạng chuẩn: Tên tour _tên schedule
                };

                using var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", token);

                // 👉 Bước A: Ra lệnh cho SocialAPI tạo Group Chat
                var response = await client.PostAsJsonAsync(
                    "https://localhost:7010/api/chat/rooms/schedule",
                    chatRoomRequest
                );

                // 👉 Bước B: Nếu tạo phòng thành công và tìm thấy Manager ID, tự động add Manager vào luôn
                if (response.IsSuccessStatusCode && !string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int managerId))
                {
                    var addMemberRequest = new { userIds = new List<int> { managerId } };
                    await client.PostAsJsonAsync(
                        $"https://localhost:7010/api/chat/rooms/schedule/{scheduleId}/members",
                        addMemberRequest
                    );
                    _logger.LogInformation($"Manager {managerId} automatically added to chat room for schedule {scheduleId}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error when creating chat room or adding manager for schedule {scheduleId}: {ex.Message}");
            }
        }
        public async Task<ReadTourScheduleDTO> UpdateScheduleAsync(int id, UpdateTourScheduleDTO dto)
        {
            var hasOrders = await _bookingApiClient.HasOrdersForScheduleAsync(id);
            if (hasOrders)
                throw new InvalidOperationException("Cannot update this schedule because it already has active orders.");

            if (dto.DepartureDate >= dto.ReturnDate)
            {
                throw new Exception("Departure date must be before the return date.");
            }

            await ValidateScheduleDuration(dto.TourId, dto.DepartureDate, dto.ReturnDate);

            var existingSchedule = await _scheduleRepo.GetByIdAsync(id);
            if (existingSchedule == null) throw new Exception("Tour schedule not found.");
            if (existingSchedule.TourId != dto.TourId)
                throw new Exception("Cannot change the tour of an existing schedule.");
            _mapper.Map(dto, existingSchedule);

            await _scheduleRepo.UpdateAsync(existingSchedule);
            return _mapper.Map<ReadTourScheduleDTO>(existingSchedule);
        }

        public async Task DeleteScheduleAsync(int id)
        {
            var hasOrders = await _bookingApiClient.HasOrdersForScheduleAsync(id);
            if (hasOrders)
                throw new InvalidOperationException("Cannot delete this schedule because it already has active orders.");
            var schedule = await _scheduleRepo.GetByIdAsync(id);
            if (schedule == null) throw new Exception("Tour schedule not found.");

            await _scheduleRepo.DeleteAsync(schedule);
        }

        public async Task<List<ItineraryLocationDto>> GetItinerariesByScheduleIdAsync(int scheduleId)
        {
            var schedule = await _scheduleRepo.GetByIdAsync(scheduleId);
            if (schedule == null)
            {
                throw new Exception("Tour schedule not found.");
            }

            var query = schedule.TourScheduleItineraries?.AsEnumerable() ?? Enumerable.Empty<TourScheduleItinerary>();
            var result = query.Where(x => x.ScheduleId == scheduleId)
                              .OrderBy(x => x.DayNumber)
                              .ThenBy(x => x.StartDuration).ToList();
            return _mapper.Map<List<ItineraryLocationDto>>(result);
        }

        public async Task<TourRouteDto> GetTourRouteAsync(int scheduleId)
        {
            var schedule = await _scheduleRepo.GetScheduleWithItineraryAsync(scheduleId);
            if (schedule == null)
            {
                throw new KeyNotFoundException($"Tour schedule with id {scheduleId} not found.");
            }

            var routeDto = new TourRouteDto
            {
                ScheduleId = schedule.Id,
                TourName = schedule.Tour?.Name
            };

            if (schedule.TourScheduleItineraries != null && schedule.TourScheduleItineraries.Any())
            {
                var sortedItineraries = schedule.TourScheduleItineraries
                    .OrderBy(x => x.DayNumber)
                    .ThenBy(x => x.StartDuration)
                    .ToList();

                int sequence = 1;
                foreach (var item in sortedItineraries)
                {
                    // Chỉ map các điểm đã khai báo toạ độ hợp lệ
                    if (item.LocationLat.HasValue && item.LocationLng.HasValue)
                    {
                        routeDto.Waypoints.Add(new WaypointDto
                        {
                            Name = item.LocationName,
                            Lat = item.LocationLat.Value,
                            Lng = item.LocationLng.Value,
                            Sequence = sequence++
                        });

                        routeDto.GeometryCoordinates.Add(new List<double> { item.LocationLng.Value, item.LocationLat.Value });
                    }
                }
            }

            return routeDto;
        }

        public async Task<IEnumerable<ReadTourScheduleDTO>> GetSchedulesByIdsAsync(IEnumerable<int> scheduleIds)
        {
            var schedules = await _scheduleRepo.GetByIdsAsync(scheduleIds.ToList());
            return _mapper.Map<IEnumerable<ReadTourScheduleDTO>>(schedules);
        }

        public async Task<PaginationDTO<ReadTourScheduleDTO>> GetSchedulesByCreatedByAsync(int userId, int page, int pageSize)
        {
            var schedules = await _scheduleRepo.GetByCreatedByAsync(userId, page, pageSize);
            var total = await _scheduleRepo.CountByCreatedByAsync(userId);

            return new PaginationDTO<ReadTourScheduleDTO>
            {
                Data = _mapper.Map<List<ReadTourScheduleDTO>>(schedules),
                CurrentPage = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
        }

        public Task<List<int>> GetScheduleIdsByCreatedByAsync(int userId)
        {
            return _scheduleRepo.GetIdsByCreatedByAsync(userId);
        }

        private async Task ValidateScheduleDuration(int tourId, DateTime departureDate, DateTime returnDate)
        {
            var tour = await _tourRepo.GetById(tourId);
            if (tour != null && tour.TourItineraries != null && tour.TourItineraries.Any())
            {
                int maxDays = tour.TourItineraries.Max(i => i.DayNumber);
                var expectedReturnDate = departureDate.Date.AddDays(maxDays);

                if (returnDate.Date != expectedReturnDate.Date)
                {
                    throw new Exception($"Invalid Return Date. Based on the tour itinerary ({maxDays} days), the return date must be {expectedReturnDate:yyyy-MM-dd}.");
                }
            }
        }
    }
}
