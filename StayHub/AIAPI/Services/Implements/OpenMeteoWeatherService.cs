using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Localization;
using AIAPI.Settings;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;

namespace AIAPI.Services.Implements;

public class OpenMeteoWeatherService : IWeatherService
{
    private readonly HttpClient _httpClient;
    private readonly WeatherSettings _settings;
    private readonly IAiLocalizedCopy _text;
    private readonly IMemoryCache _cache;

    public OpenMeteoWeatherService(
        HttpClient httpClient,
        IOptions<WeatherSettings> settings,
        IAiLocalizedCopy text,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _text = text;
        _cache = cache;
    }

    public async Task<WeatherAdviceDTO?> GetTravelWeatherAdviceAsync(
        string city,
        DateTime startDate,
        DateTime? endDate,
        double? latitude = null,
        double? longitude = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCity = (city ?? "").Trim().ToLowerInvariant();
        var cacheKey = $"weather_{normalizedCity}_{startDate:yyyyMMdd}_{(endDate ?? startDate.AddDays(2)):yyyyMMdd}";
        if (_cache.TryGetValue(cacheKey, out var cachedValue) && cachedValue is WeatherAdviceDTO cachedAdvice)
        {
            Console.WriteLine($"[Weather Cache Hit] City={city}");
            return cachedAdvice;
        }

        double lat;
        double lng;

        if (latitude.HasValue && longitude.HasValue)
        {
            lat = latitude.Value;
            lng = longitude.Value;
        }
        else
        {
            var geo = VietnamCityGeoResolver.Resolve(city);
            if (geo == null)
            {
                return null;
            }
            lat = geo.Latitude;
            lng = geo.Longitude;
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
                lat,
                lng,
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
                lat,
                lng,
                historicalStart,
                historicalEnd,
                cancellationToken);
        }

        if (payload?.Daily == null || payload.Daily.Time.Count == 0)
        {
            Console.WriteLine($"[OpenMeteo Return] Null payload.Daily for {city}");
            return null;
        }

        var avgMax = payload.Daily.TemperatureMax.Average();
        var avgMin = payload.Daily.TemperatureMin.Average();
        var totalRain = payload.Daily.PrecipitationSum.Sum();
        var rainyDays = payload.Daily.PrecipitationSum.Count(p => p >= 5);

        var summary = dataSource == "forecast"
            ? _text.WeatherForecastSummary(city, avgMin, avgMax, totalRain, payload.Daily.Time.Count)
            : _text.WeatherHistoricalSummary(city, avgMin, avgMax, totalRain);

        var impact = BuildImpact(totalRain, rainyDays, avgMax, payload.Daily.Time.Count);

        var advice = new WeatherAdviceDTO
        {
            City = city,
            DataSource = dataSource,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            AvgMaxTempC = Math.Round(avgMax, 1),
            AvgMinTempC = Math.Round(avgMin, 1),
            TotalRainMm = Math.Round(totalRain, 1),
            Summary = summary,
            ImpactOnTours = impact
        };
        
        Console.WriteLine($"[OpenMeteo Return] City={advice.City}, AvgMax={advice.AvgMaxTempC}");
        _cache.Set(cacheKey, advice, TimeSpan.FromHours(2));
        return advice;
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
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[OpenMeteo Error] Status: {response.StatusCode}, Url: {url}, Response: {err}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        Console.WriteLine($"[OpenMeteo Success] Url: {url}, Response: {json}");
        
        try 
        {
            return System.Text.Json.JsonSerializer.Deserialize<OpenMeteoDailyResponse>(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpenMeteo Exception] {ex.Message}");
            return null;
        }
    }

    private string BuildImpact(double totalRain, int rainyDays, double avgMax, int dayCount)
    {
        if (rainyDays >= Math.Max(2, dayCount / 2) || totalRain >= 40)
        {
            return _text.WeatherImpactRainy;
        }

        if (avgMax >= 34)
        {
            return _text.WeatherImpactHot;
        }

        if (avgMax <= 22)
        {
            return _text.WeatherImpactCool;
        }

        return _text.WeatherImpactMild;
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
