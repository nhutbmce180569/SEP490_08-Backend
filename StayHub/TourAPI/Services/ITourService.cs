using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Services
{
    public interface ITourService
    {
        Task Add(CreateTourDTO model, int createdBy);
        Task<PaginationDTO<ReadTourDTO>> GetByAdmin(int page, int pageSize, string? searchTerm = null, int? managerId = null);
        Task<PaginationDTO<ReadTourDTO>> GetByManager(int managerId, int page, int pageSize, string? searchTerm = null);
        Task ChangeManagerAsync(int tourId, int newManagerId, int updatedBy);
        Task UpdateTourStatusAsync(int id, string status);
        Task<PaginationDTO<ReadTourDTO>> GetAll(int page, int pageSize, int userId, bool isAdmin, string? searchTerm = null, int? categoryId = null, bool createdByMe = false);
        Task<PaginationDTO<ReadTourDTO>> SearchTours(int page, int pageSize, string? searchTerm = null, int? categoryId = null, string? country = null, string? city = null, long? minPrice = null, long? maxPrice = null, DateTime? startDate = null, DateTime? endDate = null, int? duration = null, string? sortBy = null);
        Task<ReadTourDTO> GetActiveTour(int id);
        Task<PaginationDTO<ReadTourDTO>> GetActiveTours(int page, int pageSize);
        Task<ReadTourDTO> GetById(int id, int? userId = null, bool isAdmin = false);
        Task Update(int id, UpdateTourDTO model, int updatedBy);
        Task Delete(int id);
        Task ActiveTour(int id, bool isActive);
        Task<int> CountToursByCategoryIdAsync(int categoryId);
        Task<List<ItineraryLocationDto>> GetItinerariesByTourIdAsync(int tourId);
        Task<IEnumerable<ReadTourDTO>> GetToursByIdsAsync(IEnumerable<int> tourIds);

    }
}
