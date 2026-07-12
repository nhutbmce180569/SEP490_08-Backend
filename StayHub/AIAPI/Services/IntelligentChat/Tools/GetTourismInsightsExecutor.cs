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
    public class GetTourismInsightsExecutor : IToolExecutor
    {
        private readonly ITourSemanticSearchService _searchService;
        private readonly ILogger<GetTourismInsightsExecutor> _logger;

        public GetTourismInsightsExecutor(
            ITourSemanticSearchService searchService,
            ILogger<GetTourismInsightsExecutor> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        public string FunctionName => "search_tourism_insights";

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

            _logger.LogInformation("Calling local TourSemanticSearchService.SearchTourismAsync for '{Query}'...", query);
            var insights = await _searchService.SearchTourismAsync(query, city, 4, cancellationToken);

            var list = new List<TourismInsightDTO>();
            var simplifiedInsights = new JsonArray();
            if (insights != null)
            {
                foreach (var ins in insights)
                {
                    list.Add(ins);
                    simplifiedInsights.Add(new JsonObject
                    {
                        ["name"] = ins.Name,
                        ["type"] = ins.Type,
                        ["description"] = ins.Description
                    });
                }
            }

            var responseData = new JsonObject
            {
                ["insightsCount"] = insights?.Count ?? 0,
                ["insights"] = simplifiedInsights
            };

            return new ToolExecutionResult
            {
                ResponseData = responseData,
                TourismInsights = list
            };
        }
    }
}
