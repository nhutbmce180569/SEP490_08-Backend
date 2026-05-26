using System.Net.Http;

namespace TourAPI.Services.Implements
{
    public class CategoryService : ICategoryService
    {
        private readonly HttpClient _httpClient;
        public CategoryService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<bool> CheckCategoryExist(int categoryId)
        {
            var categoryExists = await _httpClient
           .GetAsync($"https://localhost:7010/api/categories/{categoryId}");

            return categoryExists.IsSuccessStatusCode;

        }
    }
}
