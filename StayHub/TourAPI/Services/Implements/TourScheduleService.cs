using AutoMapper;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class TourScheduleService : ITourScheduleService
    {
        private readonly ITourScheduleRepository _scheduleRepo;
        private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<TourScheduleService> _logger;

        public TourScheduleService(ITourScheduleRepository scheduleRepo, IMapper mapper, IHttpClientFactory httpClientFactory, ILogger<TourScheduleService> logger)
        {
            _scheduleRepo = scheduleRepo;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<PaginationDTO<ReadTourScheduleDTO>> GetAllSchedulesAsync(int page, int pageSize)
        {
            var schedules = await _scheduleRepo.GetAllAsync();

            var list = _mapper.Map<List<ReadTourScheduleDTO>>(schedules);

            int total = list.Count;

            list = list
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

            var result = new PaginationDTO<ReadTourScheduleDTO>
            {
                Data = list,
                CurrentPage = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };

            return result;
        }

        public async Task<ReadTourScheduleDTO> GetScheduleByIdAsync(int id)
        {
            var schedule = await _scheduleRepo.GetByIdAsync(id);
            if (schedule == null) throw new Exception("Tour schedule not found.");

            return _mapper.Map<ReadTourScheduleDTO>(schedule);
        }

        public async Task<ReadTourScheduleDTO> CreateScheduleAsync(CreateTourScheduleDTO dto)
        {
            if (dto.DepartureDate >= dto.ReturnDate)
            {
                throw new Exception("Departure date must be before the return date.");
            }

            var schedule = _mapper.Map<TourSchedule>(dto);

            await _scheduleRepo.AddAsync(schedule);
            var resultDto = _mapper.Map<ReadTourScheduleDTO>(schedule);

            // After successfully creating the schedule, create a chat room asynchronously
            // This call should not crash the main flow if it fails
            try
            {
                await CreateChatRoomForScheduleAsync(schedule.Id, schedule.Tour?.Name ?? "Tour Schedule");
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - the schedule was already created successfully
                _logger.LogError(ex, $"Failed to create chat room for schedule {schedule.Id}. Error: {ex.Message}");
            }

            return resultDto;
        }

        private async Task CreateChatRoomForScheduleAsync(int scheduleId, string tourName)
        {
            try
            {
                var chatRoomRequest = new
                {
                    scheduleId = scheduleId,
                    roomName = $"Group Chat Tour - {tourName}"
                };

                using var client = _httpClientFactory.CreateClient();
                var response = await client.PostAsJsonAsync(
                    "https://localhost:7010/api/chat/rooms/schedule",
                    chatRoomRequest
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to create chat room for schedule {scheduleId}. Status: {response.StatusCode}, Content: {errorContent}");
                }
                else
                {
                    _logger.LogInformation($"Chat room created successfully for schedule {scheduleId}");
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP error when creating chat room for schedule {scheduleId}: {ex.Message}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, $"Timeout when creating chat room for schedule {scheduleId}: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error when creating chat room for schedule {scheduleId}: {ex.Message}");
            }
        }

        public async Task<ReadTourScheduleDTO> UpdateScheduleAsync(int id, UpdateTourScheduleDTO dto)
        {
            if (dto.DepartureDate >= dto.ReturnDate)
            {
                throw new Exception("Departure date must be before the return date.");
            }

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

        public async Task<IEnumerable<ReadTourScheduleDTO>> GetSchedulesByIdsAsync(IEnumerable<int> scheduleIds)
        {
            var schedules = await _scheduleRepo.GetAllAsync();
            var filtered = schedules.Where(s => scheduleIds.Contains(s.Id)).ToList();
            return _mapper.Map<IEnumerable<ReadTourScheduleDTO>>(filtered);
        }
    }
}
