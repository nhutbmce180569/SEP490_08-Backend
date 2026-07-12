using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AIAPI.DTOs;
using AIAPI.Localization;
using AIAPI.Services;
using AIAPI.Services.IntelligentChat.Models;
using AIAPI.Services.IntelligentChat.Prompts;
using AIAPI.Services.IntelligentChat.Providers;
using AIAPI.Services.IntelligentChat.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIAPI.Services.Implements
{
    public class IntelligentChatService : IIntelligentChatService
    {
        private const int MaxTurns = 5;
        private const int MaxHistory = 6;

        private readonly IChatProviderFactory _providerFactory;
        private readonly IPromptBuilder _promptBuilder;
        private readonly ToolDefinitionFactory _toolDefinitionFactory;
        private readonly IEnumerable<IToolExecutor> _toolExecutors;
        private readonly IAiCultureAccessor _cultureAccessor;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IntelligentChatService> _logger;

        public IntelligentChatService(
            IChatProviderFactory providerFactory,
            IPromptBuilder promptBuilder,
            ToolDefinitionFactory toolDefinitionFactory,
            IEnumerable<IToolExecutor> toolExecutors,
            IAiCultureAccessor cultureAccessor,
            IConfiguration configuration,
            ILogger<IntelligentChatService> logger)
        {
            _providerFactory = providerFactory;
            _promptBuilder = promptBuilder;
            _toolDefinitionFactory = toolDefinitionFactory;
            _toolExecutors = toolExecutors;
            _cultureAccessor = cultureAccessor;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IntelligentChatResponseDTO> ChatAsync(IntelligentChatRequestDTO request, CancellationToken cancellationToken = default)
        {
            var providerName = _configuration["LLM:Provider"] ?? "Gemini";
            var chatProvider = _providerFactory.GetProvider(providerName);

            var isHuggingFace = providerName.Equals("HuggingFace", StringComparison.OrdinalIgnoreCase);
            var toolsJson = isHuggingFace
                ? _toolDefinitionFactory.GetOpenAiToolsDefinition()
                : _toolDefinitionFactory.GetToolsDefinition();

            var systemInstruction = _promptBuilder.BuildSalesPrompt(_cultureAccessor.IsVietnamese);
            var executorsDict = _toolExecutors.ToDictionary(e => e.FunctionName, e => e);

            var chatMessages = new List<ChatMessage>();
            if (request.History != null && request.History.Count > 0)
            {
                var historyToKeep = request.History.TakeLast(MaxHistory).ToList();
                foreach (var h in historyToKeep)
                {
                    chatMessages.Add(new ChatMessage
                    {
                        Role = h.Role == "assistant" ? "model" : h.Role,
                        Content = h.Content
                    });
                }
            }

            chatMessages.Add(new ChatMessage
            {
                Role = "user",
                Content = request.Message
            });

            var recommendedTours = new List<TourRecommendationItemDTO>();
            WeatherAdviceDTO? weatherAdvice = null;
            var culturalFacts = new List<CulturalFactDTO>();
            var tourismInsights = new List<TourismInsightDTO>();

            int currentTurn = 0;
            string finalReply = "";

            while (currentTurn < MaxTurns)
            {
                currentTurn++;

                var providerRequest = new ProviderChatRequest
                {
                    Messages = chatMessages,
                    ToolsDefinition = toolsJson
                };

                _logger.LogInformation("Sending request to LLM Provider {Provider} (Turn {Turn})...", providerName, currentTurn);

                ProviderChatResponse providerResponse;
                try
                {
                    providerResponse = await chatProvider.ChatAsync(providerRequest, systemInstruction, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LLM provider {Provider} failed. Attempting fallback to Hugging Face...", providerName);
                    
                    // Fallback mechanism to Hugging Face if Gemini fails or is rate-limited
                    if (!isHuggingFace)
                    {
                        try
                        {
                            var fallbackProvider = _providerFactory.GetProvider("HuggingFace");
                            var fallbackToolsJson = _toolDefinitionFactory.GetOpenAiToolsDefinition();
                            
                            var fallbackRequest = new ProviderChatRequest
                            {
                                Messages = chatMessages,
                                ToolsDefinition = fallbackToolsJson
                            };

                            providerResponse = await fallbackProvider.ChatAsync(fallbackRequest, systemInstruction, cancellationToken);
                        }
                        catch (Exception fallbackEx)
                        {
                            _logger.LogError(fallbackEx, "Hugging Face fallback also failed.");
                            throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }

                if (providerResponse.HasToolCalls)
                {
                    // Add model's turn to chat messages history
                    chatMessages.Add(new ChatMessage
                    {
                        Role = "model",
                        ToolCalls = providerResponse.ToolCalls
                    });

                    foreach (var toolCall in providerResponse.ToolCalls!)
                    {
                        _logger.LogInformation("Executing tool: {FunctionName} (ID: {CallId})", toolCall.Name, toolCall.Id);

                        JsonObject? functionArgs = null;
                        if (!string.IsNullOrWhiteSpace(toolCall.ArgumentsJson))
                        {
                            try
                            {
                                functionArgs = JsonSerializer.Deserialize<JsonObject>(toolCall.ArgumentsJson);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to parse arguments for tool {FunctionName}", toolCall.Name);
                            }
                        }

                        JsonObject toolResponseData;
                        if (executorsDict.TryGetValue(toolCall.Name, out var executor))
                        {
                            try
                            {
                                var executionResult = await executor.ExecuteAsync(functionArgs, cancellationToken);
                                toolResponseData = executionResult.ResponseData;

                                if (executionResult.Recommendations != null)
                                {
                                    recommendedTours.AddRange(executionResult.Recommendations);
                                }
                                if (executionResult.Weather != null)
                                {
                                    weatherAdvice = executionResult.Weather;
                                }
                                if (executionResult.CulturalFacts != null)
                                {
                                    culturalFacts.AddRange(executionResult.CulturalFacts);
                                }
                                if (executionResult.TourismInsights != null)
                                {
                                    tourismInsights.AddRange(executionResult.TourismInsights);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error executing local tool {FunctionName}", toolCall.Name);
                                toolResponseData = new JsonObject { ["error"] = ex.Message };
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Unknown function requested: {FunctionName}", toolCall.Name);
                            toolResponseData = new JsonObject { ["error"] = $"Unknown function: {toolCall.Name}" };
                        }

                        chatMessages.Add(new ChatMessage
                        {
                            Role = "tool",
                            ToolCallId = toolCall.Id,
                            Name = toolCall.Name,
                            Content = JsonSerializer.Serialize(toolResponseData)
                        });
                    }

                    continue;
                }

                finalReply = providerResponse.Content;
                break;
            }

            var suggestedQuestions = _cultureAccessor.IsVietnamese
                ? new List<string>
                  {
                      "Có những tour du lịch nào đang hot?",
                      "Thời tiết Đà Lạt tuần tới thế nào?",
                      "Tôi muốn đặt tour đi Nha Trang"
                  }
                : new List<string>
                  {
                      "What are some trending tours?",
                      "How is the weather in Da Lat next week?",
                      "I want to book a tour to Nha Trang"
                  };

            return new IntelligentChatResponseDTO
            {
                SessionId = request.SessionId ?? Guid.NewGuid().ToString("N"),
                Reply = finalReply,
                RecommendedTours = recommendedTours,
                WeatherAdvice = weatherAdvice,
                CulturalFacts = culturalFacts,
                TourismInsights = tourismInsights,
                SuggestedQuestions = suggestedQuestions
            };
        }
    }
}
