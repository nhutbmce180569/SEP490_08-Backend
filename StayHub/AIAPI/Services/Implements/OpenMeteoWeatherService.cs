using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Settings;
using Microsoft.Extensions.Options;

namespace AIAPI.Services.Implements;

public class OpenMeteoWeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly WeatherSettings _settings;

    public OpenMeteoWeatherService(HttpClient httpClient, IOptions<WeatherSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<WeatherAdviceDTO?> GetTravelWeatherAdviceAsync(
        string city,
        DateTime startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        var geo = VietnamCityGeoResolver.Resolve(city);
        if (geo == null)
        {
            return null;
        }

        var periodStart = startDate.Date;
        var periodEnd = (endDate ?? startDate.AddDays(2)).Date;
        if (periodEnd < periodStart)
        {
            periodEnd = periodStart;
        }

        var today = DateTime.UtcNow.Date;
        var daysAhead = (periodStart - today).Days;
        var useForecast = daysAhead >= 0 && daysAhead <= _settings.ForecastMaxDaysAhead;

        OpenMeteoDailyResponse? payload;
        string dataSource;

        if (useForecast)
        {
            dataSource = "forecast";
            payload = await FetchDailyAsync(
                _settings.OpenMeteoForecastUrl,
                geo.Latitude,
                geo.Longitude,
                periodStart,
                periodEnd,
                cancellationToken);
        }
        else
        {
            dataSource = "historical";
            var historicalStart = periodStart.AddYears(-1);
            var historicalEnd = periodEnd.AddYears(-1);
            payload = await FetchDailyAsync(
                _settings.OpenMeteoArchiveUrl,
                geo.Latitude,
                geo.Longitude,
                historicalStart,
                historicalEnd,
                cancellationToken);
        }

        if (payload?.Daily == null || payload.Daily.Time.Count == 0)
        {
            return null;
        }

        var avgMax = payload.Daily.TemperatureMax.Average();
        var avgMin = payload.Daily.TemperatureMin.Average();
        var totalRain = payload.Daily.PrecipitationSum.Sum();
        var rainyDays = payload.Daily.PrecipitationSum.Count(p => p >= 5);

        var summary = dataSource == "forecast"
            ? $"Dự báo thời tiết {geo.DisplayName}: nhiệt độ {avgMin:0.#}–{avgMax:0.#}°C, mưa tích lũy ~{totalRain:0.#}mm trong {payload.Daily.Time.Count} ngày."
            : $"Thống kê thời tiết lịch sử (cùng kỳ năm trước) tại {geo.DisplayName}: {avgMin:0.#}–{avgMax:0.#}°C, mưa ~{totalRain:0.#}mm.";

        var impact = BuildImpact(totalRain, rainyDays, avgMax, payload.Daily.Time.Count);

        return new WeatherAdviceDTO
        {
            City = geo.DisplayName,
            DataSource = dataSource,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            AvgMaxTempC = Math.Round(avgMax, 1),
            AvgMinTempC = Math.Round(avgMin, 1),
            TotalRainMm = Math.Round(totalRain, 1),
            Summary = summary,
            ImpactOnTours = impact
        };
    }

    private async Task<OpenMeteoDailyResponse?> FetchDailyAsync(
        string baseUrl,
        double lat,
        double lng,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        var url =
            $"{baseUrl}?latitude={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&longitude={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
            $"&daily=temperature_2m_max,temperature_2m_min,precipitation_sum,weathercode" +
            $"&timezone=Asia%2FHo_Chi_Minh" +
            $"&start_date={start:yyyy-MM-dd}&end_date={end:yyyy-MM-dd}";

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<OpenMeteoDailyResponse>(cancellationToken);
    }

    private static string BuildImpact(double totalRain, int rainyDays, double avgMax, int dayCount)
    {
        if (rainyDays >= Math.Max(2, dayCount / 2) || totalRain >= 40)
        {
            return "Thời điểm này thường mưa nhiều — ưu tiên tour trong nhà, chợ nổi buổi sáng sớm, mang áo mưa và ưu tiên lịch trình linh hoạt.";
        }

        if (avgMax >= 34)
        {
            return "Nhiệt độ cao — chọn tour có giờ khởi hành sớm, nghỉ trưa hợp lý, bổ sung nước và ưu tiên biển/đêm.";
        }

        if (avgMax <= 22)
        {
            return "Thời tiết mát — phù hợp trekking nhẹ, tham quan văn hóa; nên mang thêm áo khoác mỏng.";
        }

        return "Thời tiết ổn định — phù hợp hầu hết tour ngoài trời, tắm biển và tham quan điểm văn hóa.";
    }

    private sealed class OpenMeteoDailyResponse
    {
        [JsonPropertyName("daily")]
        public OpenMeteoDaily? Daily { get; set; }
    }

    private sealed class OpenMeteoDaily
    {
        [JsonPropertyName("time")]
        public List<string> Time { get; set; } = new();

        [JsonPropertyName("temperature_2m_max")]
        public List<double> TemperatureMax { get; set; } = new();

        [JsonPropertyName("temperature_2m_min")]
        public List<double> TemperatureMin { get; set; } = new();

        [JsonPropertyName("precipitation_sum")]
        public List<double> PrecipitationSum { get; set; } = new();
    }
}
