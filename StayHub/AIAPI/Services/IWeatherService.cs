using AIAPI.DTOs;

namespace AIAPI.Services;

public interface IWeatherService
{
    Task<WeatherAdviceDTO?> GetTravelWeatherAdviceAsync(
        string city,
        DateTime startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default);
}
