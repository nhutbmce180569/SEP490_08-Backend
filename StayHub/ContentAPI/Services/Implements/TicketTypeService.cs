using AutoMapper;
using ContentAPI.DTOs;
using ContentAPI.Models;
using ContentAPI.Repositories;

namespace ContentAPI.Services.Implements
{
    public class TicketTypeService : ITicketTypeService
    {
        private readonly ITicketTypeRepository _ticketTypeRepository;
        private readonly IMapper _mapper;

        public TicketTypeService(ITicketTypeRepository ticketTypeRepository, IMapper mapper)
        {
            _ticketTypeRepository = ticketTypeRepository;
            _mapper = mapper;
        }

        public async Task<PaginationDTO<ReadTicketTypeDTO>> GetAllTicketTypes(int page, int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var ticketTypes = await _ticketTypeRepository.GetAllAsync();
            var total = ticketTypes.Count;
            var pagedTicketTypes = ticketTypes
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PaginationDTO<ReadTicketTypeDTO>
            {
                Data = _mapper.Map<List<ReadTicketTypeDTO>>(pagedTicketTypes),
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<List<ReadTicketTypeDTO>> GetActiveTicketTypes()
        {
            var ticketTypes = await _ticketTypeRepository.GetActiveAsync();
            return _mapper.Map<List<ReadTicketTypeDTO>>(ticketTypes);
        }

        public async Task<ReadTicketTypeDTO?> GetTicketTypeById(int id)
        {
            var ticketType = await _ticketTypeRepository.GetById(id);
            if (ticketType == null) return null;

            return _mapper.Map<ReadTicketTypeDTO>(ticketType);
        }

        public async Task<ReadTicketTypeDTO> CreateTicketType(CreateTicketTypeDTO dto)
        {
            var ticketType = new TicketType
            {
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive ?? true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _ticketTypeRepository.Add(ticketType);
            return _mapper.Map<ReadTicketTypeDTO>(ticketType);
        }

        public async Task<bool> UpdateTicketType(int id, UpdateTicketTypeDTO dto)
        {
            var existingTicketType = await _ticketTypeRepository.GetById(id);
            if (existingTicketType == null) return false;

            existingTicketType.Name = dto.Name;
            existingTicketType.Description = dto.Description;
            if (dto.IsActive.HasValue) existingTicketType.IsActive = dto.IsActive.Value;
            existingTicketType.UpdatedAt = DateTime.Now;

            await _ticketTypeRepository.Update(existingTicketType);
            return true;
        }

        public async Task<bool> ChangeTicketTypeStatus(int id, bool isActive)
        {
            var existingTicketType = await _ticketTypeRepository.GetById(id);
            if (existingTicketType == null) return false;

            existingTicketType.IsActive = isActive;
            existingTicketType.UpdatedAt = DateTime.Now;

            await _ticketTypeRepository.Update(existingTicketType);
            return true;
        }
    }
}
