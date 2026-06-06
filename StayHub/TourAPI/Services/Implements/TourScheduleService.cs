using AutoMapper;
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

        public TourScheduleService(ITourScheduleRepository scheduleRepo, ITourRepository tourRepo, IMapper mapper)
        {
            _scheduleRepo = scheduleRepo;
            _tourRepo = tourRepo;
            _mapper = mapper;
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
            return _mapper.Map<ReadTourScheduleDTO>(schedule);
        }

        public async Task<ReadTourScheduleDTO> UpdateScheduleAsync(int id, UpdateTourScheduleDTO dto)
        {
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
