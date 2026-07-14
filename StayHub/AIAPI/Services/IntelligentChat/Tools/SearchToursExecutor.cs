using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AIAPI.DTOs;
using AIAPI.Services;
using AIAPI.Services.IntelligentChat.Models;
using Microsoft.Extensions.Logging;

namespace AIAPI.Services.IntelligentChat.Tools
{
    public class SearchToursExecutor : IToolExecutor
    {
        private readonly ITourSemanticSearchService _searchService;
        private readonly ILogger<SearchToursExecutor> _logger;

        public SearchToursExecutor(
            ITourSemanticSearchService searchService,
            ILogger<SearchToursExecutor> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        public string FunctionName => "search_tours";

        public async Task<ToolExecutionResult> ExecuteAsync(JsonObject? args, CancellationToken cancellationToken)
        {
            if (args == null)
            {
                return new ToolExecutionResult
                {
                    ResponseData = new JsonObject { ["error"] = "Arguments are missing." }
                };
            }

            var query = args["query"]?.ToString() ?? "";
            var city = args["city"]?.ToString();
            var minPrice = args["minPrice"]?.GetValue<long>();
            var maxPrice = args["maxPrice"]?.GetValue<long>();
            var durationDays = args["durationDays"]?.GetValue<int>();

            var searchRequest = new NaturalLanguageSearchRequestDTO
            {
                Query = query,
                City = city,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                DurationDays = durationDays,
                Top = 6
            };

            _logger.LogInformation("Calling local TourSemanticSearchService with query '{Query}'...", query);
            var searchResults = await _searchService.SearchAsync(searchRequest, cancellationToken);

            var recommendations = new List<TourRecommendationItemDTO>();
            var simplifiedTours = new JsonArray();
            if (searchResults != null)
            {
                foreach (var r in searchResults)
                {
                    recommendations.Add(r);
                    simplifiedTours.Add(new JsonObject
                    {
                        ["id"] = r.TourId,
                        ["name"] = r.Name,
                        ["price"] = r.MinPrice,
                        ["durationDays"] = r.DurationDays,
                        ["score"] = r.Score
                    });
                }
            }

            var responseData = new JsonObject
            {
                ["resultsCount"] = searchResults?.Count ?? 0,
                ["tours"] = simplifiedTours
            };

            return new ToolExecutionResult
            {
                ResponseData = responseData,
                Recommendations = recommendations
            };
        }
    }
}
