using System.Net.Http;
using System.Text.Json;

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
                .GetAsync($"api/categories/{categoryId}");

            return categoryExists.IsSuccessStatusCode;
        }

        public async Task<Dictionary<int, string>> GetCategoryNamesAsync(IEnumerable<int> categoryIds)
        {
            var ids = categoryIds.Distinct().ToHashSet();
            var map = new Dictionary<int, string>();
            if (ids.Count == 0)
            {
                return map;
            }

            try
            {
                var response = await _httpClient.GetAsync(
                    "api/categories?page=1&pageSize=500");

                if (!response.IsSuccessStatusCode)
                {
                    return map;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!TryGetCategoryArray(root, out var categories))
                {
                    return map;
                }

                foreach (var item in categories.EnumerateArray())
                {
                    if (!TryReadCategory(item, out var id, out var name))
                    {
                        continue;
                    }

                    if (ids.Contains(id) && !string.IsNullOrWhiteSpace(name))
                    {
                        map[id] = name;
                    }
                }
            }
            catch
            {
                // Fall back to generic labels in the repository.
            }

            return map;
        }

        private static bool TryGetCategoryArray(JsonElement root, out JsonElement categories)
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                categories = root;
                return true;
            }

            if (root.TryGetProperty("data", out var dataProp))
            {
                if (dataProp.ValueKind == JsonValueKind.Array)
                {
                    categories = dataProp;
                    return true;
                }

                if (dataProp.ValueKind == JsonValueKind.Object &&
                    dataProp.TryGetProperty("data", out var nestedData) &&
                    nestedData.ValueKind == JsonValueKind.Array)
                {
                    categories = nestedData;
                    return true;
                }

                if (dataProp.ValueKind == JsonValueKind.Object &&
                    dataProp.TryGetProperty("Data", out var nestedDataPascal) &&
                    nestedDataPascal.ValueKind == JsonValueKind.Array)
                {
                    categories = nestedDataPascal;
                    return true;
                }
            }

            if (root.TryGetProperty("Data", out var dataPascal) && dataPascal.ValueKind == JsonValueKind.Array)
            {
                categories = dataPascal;
                return true;
            }

            categories = default;
            return false;
        }

        private static bool TryReadCategory(JsonElement item, out int id, out string? name)
        {
            id = 0;
            name = null;

            if (item.TryGetProperty("id", out var idProp))
            {
                id = idProp.GetInt32();
            }
            else if (item.TryGetProperty("Id", out var idPascal))
            {
                id = idPascal.GetInt32();
            }

            if (item.TryGetProperty("name", out var nameProp))
            {
                name = nameProp.GetString();
            }
            else if (item.TryGetProperty("Name", out var namePascal))
            {
                name = namePascal.GetString();
            }

            return id > 0;
        }
    }
}
