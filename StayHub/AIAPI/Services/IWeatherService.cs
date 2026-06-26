using AIAPI.DTOs;

namespace AIAPI.Services;

public interface IWeatherService
{
    Task<WeatherAdviceDTO?> GetTravelWeatherAdviceAsync(
        string city,
        DateTime startDate,
        DateTime? endDate,
        double? latitude = null,
        double? longitude = null,
        CancellationToken cancellationToken = default);
}
