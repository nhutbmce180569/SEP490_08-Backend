namespace TourAPI.Services
{
    public interface ICategoryService
    {
        Task<bool> CheckCategoryExist(int categoryId);
    }
}
