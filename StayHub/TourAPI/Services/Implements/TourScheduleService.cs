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

        public async Task<IEnumerable<ReadTourScheduleDTO>> GetAllSchedulesAsync()
        {
            var schedules = await _scheduleRepo.GetAllAsync();
            return _mapper.Map<IEnumerable<ReadTourScheduleDTO>>(schedules);
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
    }
}