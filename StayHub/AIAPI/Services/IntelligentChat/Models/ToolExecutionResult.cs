using System.Collections.Generic;
using System.Text.Json.Nodes;
using AIAPI.DTOs;

namespace AIAPI.Services.IntelligentChat.Models
{
    public class ToolExecutionResult
    {
        public JsonObject ResponseData { get; set; } = new();
        public List<TourRecommendationItemDTO>? Recommendations { get; set; }
        public WeatherAdviceDTO? Weather { get; set; }
        public List<CulturalFactDTO>? CulturalFacts { get; set; }
        public List<TourismInsightDTO>? TourismInsights { get; set; }
    }
}
