using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AIAPI.DTOs;
using AIAPI.Services;
using AIAPI.Services.IntelligentChat.Models;
using Microsoft.Extensions.Logging;

namespace AIAPI.Services.IntelligentChat.Tools
{
    public class RecommendToursExecutor : IToolExecutor
    {
        private readonly IPersonalizedTourRecommendationService _personalizedService;
        private readonly ILogger<RecommendToursExecutor> _logger;

        public RecommendToursExecutor(
            IPersonalizedTourRecommendationService personalizedService,
            ILogger<RecommendToursExecutor> logger)
        {
            _personalizedService = personalizedService;
            _logger = logger;
        }

        public string FunctionName => "recommend_tours_from_profile";

        public async Task<ToolExecutionResult> ExecuteAsync(JsonObject? args, CancellationToken cancellationToken)
        {
            if (args == null)
            {
                return new ToolExecutionResult
                {
                    ResponseData = new JsonObject { ["error"] = "Arguments are missing." }
                };
            }

            var companionType = args["companionType"]?.ToString() ?? "solo";
            var startDateStr = args["preferredStartDate"]?.ToString() ?? DateTime.Today.ToString("yyyy-MM-dd");
            var endDateStr = args["preferredEndDate"]?.ToString();
            var maxBudget = args["maxBudgetPerPerson"]?.GetValue<long>();
            var travelPace = args["travelPace"]?.ToString() ?? "moderate";
            var adultCount = args["adultCount"]?.GetValue<int>() ?? 1;
            var childrenCount = args["childrenCount"]?.GetValue<int>() ?? 0;
            var elderlyCount = args["elderlyCount"]?.GetValue<int>() ?? 0;

            var travelInterests = new List<string>();
            if (args["travelInterests"]?.AsArray() is JsonArray interestsArray)
            {
                foreach (var node in interestsArray)
                {
                    if (node != null) travelInterests.Add(node.ToString());
                }
            }
            if (travelInterests.Count == 0)
            {
                travelInterests.Add("khám phá");
            }

            var preferredCity = args["preferredCity"]?.ToString() ?? "";
            var preferredCountry = args["preferredCountry"]?.ToString() ?? "Việt Nam";

            DateTime startDate = DateTime.TryParse(startDateStr, out var d1) ? d1 : DateTime.Today;
            DateTime? endDate = DateTime.TryParse(endDateStr, out var d2) ? d2 : null;

            var profile = new TourPreferenceQuestionnaireDTO
            {
                CompanionType = companionType,
                PreferredStartDate = startDate,
                PreferredEndDate = endDate,
                MaxBudgetPerPerson = maxBudget,
                TravelPace = travelPace,
                AdultCount = adultCount,
                ChildrenCount = childrenCount,
                ElderlyCount = elderlyCount,
                TravelInterests = travelInterests,
                NationalityType = "vietnamese",
                PreferredCity = preferredCity,
                PreferredCountry = preferredCountry,
                Top = 6
            };

            _logger.LogInformation("Calling local PersonalizedTourRecommendationService for city {City}...", preferredCity);
            var result = await _personalizedService.RecommendFromProfileAsync(profile, cancellationToken);

            var recommendations = new List<TourRecommendationItemDTO>();
            if (result.RecommendedTours != null)
            {
                recommendations.AddRange(result.RecommendedTours);
            }
            if (result.NearbyScheduleTours != null)
            {
                recommendations.AddRange(result.NearbyScheduleTours);
            }

            var culturalFacts = result.CulturalFacts ?? new List<CulturalFactDTO>();

            // Return simplified response for LLM synthesis to reduce tokens
            var simplifiedTours = new JsonArray();
            foreach (var t in recommendations.Take(3))
            {
                simplifiedTours.Add(new JsonObject
                {
                    ["id"] = t.TourId,
                    ["name"] = t.Name,
                    ["price"] = t.MinPrice,
                    ["durationDays"] = t.DurationDays,
                    ["reason"] = t.Reason
                });
            }

            var responseData = new JsonObject
            {
                ["summary"] = result.Summary,
                ["weatherSummary"] = result.WeatherAdvice?.Summary ?? "",
                ["impactOnTours"] = result.WeatherAdvice?.ImpactOnTours ?? "",
                ["recommendedTours"] = simplifiedTours
            };

            return new ToolExecutionResult
            {
                ResponseData = responseData,
                Recommendations = recommendations,
                Weather = result.WeatherAdvice,
                CulturalFacts = culturalFacts
            };
        }
    }
}
