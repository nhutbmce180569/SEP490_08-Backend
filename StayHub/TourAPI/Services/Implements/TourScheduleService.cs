using AutoMapper;
using NuGet.Protocol.Core.Types;
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

            // Thiết lập giá trị mặc định khi tạo mới
            //schedule.SoldQuantity = 0;
            //schedule.AvailableSeats = dto.MaxCapacity;

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

            //// Kiểm tra xem MaxCapacity mới có hợp lệ với số vé đã bán không
            //if (dto.MaxCapacity < existingSchedule.SoldQuantity)
            //{
            //    throw new Exception($"Cannot reduce capacity below the number of sold tickets ({existingSchedule.SoldQuantity}).");
            //}

            //_mapper.Map(dto, existingSchedule);

            //// Cập nhật lại số ghế trống theo MaxCapacity mới
            //existingSchedule.AvailableSeats = existingSchedule.MaxCapacity - (existingSchedule.SoldQuantity ?? 0);

            await _scheduleRepo.UpdateAsync(existingSchedule);
            return _mapper.Map<ReadTourScheduleDTO>(existingSchedule);
        }

        public async Task DeleteScheduleAsync(int id)
        {
            var schedule = await _scheduleRepo.GetByIdAsync(id);
            if (schedule == null) throw new Exception("Tour schedule not found.");

            //if (schedule.SoldQuantity > 0)
            //{
            //    throw new Exception("Cannot delete a schedule that already has booked tickets.");
            //}

            await _scheduleRepo.DeleteAsync(schedule);
        }

        public async Task<bool> ReserveSeatsAsync(int scheduleId, int quantity)
        {
            if (quantity <= 0) return false;

            var schedule = await _scheduleRepo.GetByIdAsync(scheduleId);
            if (schedule == null) return false;

            //if (schedule.AvailableSeats < quantity) return false;

            //var sold = schedule.SoldQuantity ?? 0;
            //schedule.SoldQuantity = sold + quantity;
            //schedule.AvailableSeats -= quantity;

            await _scheduleRepo.UpdateAsync(schedule);
            return true;
        }

        public async Task<bool> ReleaseSeatsAsync(int scheduleId, int quantity)
        {
            if (quantity <= 0) return false;

            var schedule = await _scheduleRepo.GetByIdAsync(scheduleId);
            if (schedule == null) return false;

            //var sold = schedule.SoldQuantity ?? 0;
            //if (sold < quantity) return false; // Cannot release more seats than sold

            //schedule.SoldQuantity = sold - quantity;
            //schedule.AvailableSeats += quantity;

            await _scheduleRepo.UpdateAsync(schedule);
            return true;
        }
    }
}
