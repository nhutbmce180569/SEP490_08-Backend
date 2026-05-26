namespace ContentAPI.Helpers.Implements
{
    public class TourApiClient : ITourApiClient
    {
        private readonly HttpClient _httpClient;

        public TourApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<int> GetTourCountByCategoryIdAsync(int categoryId)
        {
            var response = await _httpClient.GetAsync($"/api/tours/category/{categoryId}/count");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>();
            }

            throw new Exception($"Cannot connect to TourAPI. Status: {response.StatusCode}");
        }
    }
}
