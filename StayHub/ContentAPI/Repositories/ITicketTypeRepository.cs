using ContentAPI.Models;

namespace ContentAPI.Repositories
{
    public interface ITicketTypeRepository
    {
        Task<List<TicketType>> GetAllAsync();
        Task<List<TicketType>> GetActiveAsync();
        Task<TicketType?> GetById(int id);
        Task<TicketType> Add(TicketType ticketType);
        Task Update(TicketType ticketType);
    }
}
