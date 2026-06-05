using AutoMapper;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class TourScheduleService : ITourScheduleService
    {
        private readonly ITourScheduleRepository _scheduleRepo;
        private readonly IMapper _mapper;

        public TourScheduleService(ITourScheduleRepository scheduleRepo, IMapper mapper)
        {
            _scheduleRepo = scheduleRepo;
            _mapper = mapper;
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
            return _mapper.Map<ReadTourScheduleDTO>(schedule);
        }

        public async Task<ReadTourScheduleDTO> UpdateScheduleAsync(int id, UpdateTourScheduleDTO dto)
        {
            if (dto.DepartureDate >= dto.ReturnDate)
            {
                throw new Exception("Departure date must be before the return date.");
            }

            var existingSchedule = await _scheduleRepo.GetByIdAsync(id);
            if (existingSchedule == null) throw new Exception("Tour schedule not found.");
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