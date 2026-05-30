using AutoMapper;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class TourScheduleTicketService : ITourScheduleTicketService
    {
        private readonly ITourScheduleTicketRepository _repository;
        private readonly ITourScheduleRepository _tourScheduleRepository;
        private readonly IMapper _mapper;

        public TourScheduleTicketService(
            ITourScheduleTicketRepository repository,
            ITourScheduleRepository tourScheduleRepository,
            IMapper mapper)
        {
            _repository = repository;
            _tourScheduleRepository = tourScheduleRepository;
            _mapper = mapper;
        }

        public async Task<PaginationDTO<ReadTourScheduleTicketDTO>> GetAll(int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var entities = await _repository.GetAllAsync();
            var list = _mapper.Map<List<ReadTourScheduleTicketDTO>>(entities);
            var total = list.Count;

            list = list
                .OrderBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PaginationDTO<ReadTourScheduleTicketDTO>
            {
                Data = list,
                Total = total,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<IEnumerable<ReadTourScheduleTicketDTO>> GetByScheduleId(int scheduleId)
        {
            var schedule = await _tourScheduleRepository.GetByIdAsync(scheduleId);
            if (schedule == null) throw new Exception($"Tour schedule with Id {scheduleId} not found");

            var entities = await _repository.GetByScheduleIdAsync(scheduleId);
            return _mapper.Map<List<ReadTourScheduleTicketDTO>>(entities);
        }

        public async Task<ReadTourScheduleTicketDTO?> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ReadTourScheduleTicketDTO>(entity);
        }

        public async Task<ReadTourScheduleTicketDTO> Create(CreateTourScheduleTicketDTO dto)
        {
            await ValidateSchedule(dto.ScheduleId);
            ValidateTicketValues(dto.Price, dto.Quantity, dto.SoldQuantity ?? 0);
            await ValidateTicketTypeUniqueness(dto.ScheduleId, dto.TicketTypeId);

            var entity = _mapper.Map<TourScheduleTicket>(dto);
            ApplyComputedFields(entity, dto.Quantity, dto.SoldQuantity ?? 0, dto.IsActive);

            await _repository.AddAsync(entity);
            return _mapper.Map<ReadTourScheduleTicketDTO>(entity);
        }

        public async Task<ReadTourScheduleTicketDTO> Update(int id, UpdateTourScheduleTicketDTO dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) throw new Exception("TourScheduleTicket not found");

            if (dto.ScheduleId != entity.ScheduleId)
            {
                throw new Exception("Cannot change ScheduleId of a tour schedule ticket");
            }

            await ValidateSchedule(entity.ScheduleId);

            var soldQuantity = dto.SoldQuantity ?? entity.SoldQuantity ?? 0;
            ValidateTicketValues(dto.Price, dto.Quantity, soldQuantity);
            await ValidateTicketTypeUniqueness(entity.ScheduleId, dto.TicketTypeId, id);

            var isActive = dto.IsActive ?? entity.IsActive ?? true;

            _mapper.Map(dto, entity);
            ApplyComputedFields(entity, dto.Quantity, soldQuantity, isActive);

            await _repository.UpdateAsync(entity);
            return _mapper.Map<ReadTourScheduleTicketDTO>(entity);
        }

        public async Task Delete(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) throw new Exception("TourScheduleTicket not found");

            await _repository.DeleteAsync(entity);
        }

        private async Task ValidateSchedule(int scheduleId)
        {
            var schedule = await _tourScheduleRepository.GetByIdAsync(scheduleId);
            if (schedule == null) throw new Exception($"Tour schedule with Id {scheduleId} not found");
        }

        private async Task ValidateTicketTypeUniqueness(int scheduleId, int ticketTypeId, int? exceptId = null)
        {
            var exists = await _repository.ExistsByScheduleAndTicketTypeAsync(scheduleId, ticketTypeId, exceptId);
            if (exists)
            {
                throw new Exception($"TicketTypeId {ticketTypeId} already exists for schedule {scheduleId}");
            }
        }

        private static void ValidateTicketValues(long price, int quantity, int soldQuantity)
        {
            if (price < 0)
            {
                throw new Exception("Price must be greater than or equal to 0");
            }

            if (quantity < 0)
            {
                throw new Exception("Quantity must be greater than or equal to 0");
            }

            if (soldQuantity < 0)
            {
                throw new Exception("SoldQuantity must be greater than or equal to 0");
            }

            if (soldQuantity > quantity)
            {
                throw new Exception("SoldQuantity cannot be greater than Quantity");
            }
        }

        private static void ApplyComputedFields(
            TourScheduleTicket entity,
            int quantity,
            int soldQuantity,
            bool? isActive)
        {
            entity.Quantity = quantity;
            entity.SoldQuantity = soldQuantity;
            entity.AvailableQuantity = quantity - soldQuantity;
            entity.IsActive = isActive ?? true;
        }
    }
}
