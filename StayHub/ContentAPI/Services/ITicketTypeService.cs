using ContentAPI.DTOs;

namespace ContentAPI.Services
{
    public interface ITicketTypeService
    {
        Task<PaginationDTO<ReadTicketTypeDTO>> GetAllTicketTypes(int page, int pageSize);
        Task<List<ReadTicketTypeDTO>> GetActiveTicketTypes();
        Task<ReadTicketTypeDTO?> GetTicketTypeById(int id);
        Task<ReadTicketTypeDTO> CreateTicketType(CreateTicketTypeDTO dto);
        Task<bool> UpdateTicketType(int id, UpdateTicketTypeDTO dto);
        Task<bool> ChangeTicketTypeStatus(int id, bool isActive);
    }
}
