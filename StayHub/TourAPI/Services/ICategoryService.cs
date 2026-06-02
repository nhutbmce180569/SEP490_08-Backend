namespace TourAPI.Services
{
    public interface ICategoryService
    {
        Task<bool> CheckCategoryExist(int categoryId);
        Task<Dictionary<int, string>> GetCategoryNamesAsync(IEnumerable<int> categoryIds);
    }
}
