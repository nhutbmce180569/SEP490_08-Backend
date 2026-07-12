using System;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AIAPI.Services;
using AIAPI.Services.IntelligentChat.Models;
using Microsoft.Extensions.Logging;

namespace AIAPI.Services.IntelligentChat.Tools
{
    public class GetWeatherForecastExecutor : IToolExecutor
    {
        private readonly IWeatherService _weatherService;
        private readonly ILogger<GetWeatherForecastExecutor> _logger;

        public GetWeatherForecastExecutor(
            IWeatherService weatherService,
            ILogger<GetWeatherForecastExecutor> logger)
        {
            _weatherService = weatherService;
            _logger = logger;
        }

        public string FunctionName => "get_weather_forecast";

        public async Task<ToolExecutionResult> ExecuteAsync(JsonObject? args, CancellationToken cancellationToken)
        {
            if (args == null)
            {
                return new ToolExecutionResult
                {
                    ResponseData = new JsonObject { ["error"] = "Arguments are missing." }
                };
            }

            var city = args["city"]?.ToString() ?? "";
            var startDateStr = args["startDate"]?.ToString() ?? DateTime.Today.ToString("yyyy-MM-dd");
            var endDateStr = args["endDate"]?.ToString();

            DateTime startDate = DateTime.TryParse(startDateStr, out var d1) ? d1 : DateTime.Today;
            DateTime? endDate = DateTime.TryParse(endDateStr, out var d2) ? d2 : null;

            _logger.LogInformation("Calling local IWeatherService for city {City}...", city);
            var advice = await _weatherService.GetTravelWeatherAdviceAsync(city, startDate, endDate, cancellationToken: cancellationToken);

            JsonObject responseData;
            if (advice != null)
            {
                responseData = new JsonObject
                {
                    ["city"] = advice.City,
                    ["summary"] = advice.Summary,
                    ["impactOnTours"] = advice.ImpactOnTours,
                    ["avgMaxTempC"] = advice.AvgMaxTempC,
                    ["avgMinTempC"] = advice.AvgMinTempC,
                    ["totalRainMm"] = advice.TotalRainMm
                };
            }
            else
            {
                responseData = new JsonObject { ["error"] = "No weather data found." };
            }

            return new ToolExecutionResult
            {
                ResponseData = responseData,
                Weather = advice
            };
        }
    }
}
