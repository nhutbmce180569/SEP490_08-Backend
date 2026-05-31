namespace AIAPI.Settings;

public class WeatherSettings
{
    public const string SectionName = "Weather";
    public string OpenMeteoForecastUrl { get; set; } = "https://api.open-meteo.com/v1/forecast";
    public string OpenMeteoArchiveUrl { get; set; } = "https://archive-api.open-meteo.com/v1/archive";
    public int ForecastMaxDaysAhead { get; set; } = 14;
}
