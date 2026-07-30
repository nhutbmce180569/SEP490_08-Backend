namespace ContentAPI.Helpers
{
    public interface ITourApiClient
    {
        Task<int> GetTourCountByCategoryIdAsync(int categoryId);
    }
}
