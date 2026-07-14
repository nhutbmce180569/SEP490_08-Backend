using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AIAPI.Services.IntelligentChat.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIAPI.Services.IntelligentChat.Providers
{
    public class HuggingFaceChatProvider : IChatProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HuggingFaceChatProvider> _logger;

        public HuggingFaceChatProvider(HttpClient httpClient, IConfiguration configuration, ILogger<HuggingFaceChatProvider> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ProviderChatResponse> ChatAsync(ProviderChatRequest request, string systemInstruction, CancellationToken cancellationToken)
        {
            var hfConfig = _configuration.GetSection("LLM:HuggingFace");
            var apiKey = hfConfig["ApiKey"];
            var modelName = hfConfig["ModelName"] ?? "Qwen/Qwen2.5-72B-Instruct";
            var endpoint = hfConfig["Endpoint"] ?? "https://api-inference.huggingface.co/v1/chat/completions";

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_HF_API_KEY_HERE")
            {
                apiKey = Environment.GetEnvironmentVariable("HuggingFace__ApiKey") ?? Environment.GetEnvironmentVariable("HF_API_KEY") ?? apiKey;
            }

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_HF_API_KEY_HERE")
            {
                throw new InvalidOperationException("Hugging Face API key is not configured.");
            }

            var fallbackList = new List<string>();
            var fallbackSection = hfConfig.GetSection("FallbackModels");
            if (fallbackSection.Exists())
            {
                foreach (var child in fallbackSection.GetChildren())
                {
                    if (!string.IsNullOrEmpty(child.Value))
                    {
                        fallbackList.Add(child.Value);
                    }
                }
            }
            if (fallbackList.Count == 0)
            {
                fallbackList.AddRange(new[] {
                    "meta-llama/Llama-3.3-70B-Instruct",
                    "Qwen/Qwen2.5-Coder-32B-Instruct",
                    "meta-llama/Llama-3.1-8B-Instruct"
                });
            }

            var modelsToTry = new List<string> { modelName };
            modelsToTry.AddRange(fallbackList);
            int activeModelIndex = 0;

            var messages = new JsonArray();

            messages.Add(new JsonObject
            {
                ["role"] = "system",
                ["content"] = systemInstruction
            });

            foreach (var msg in request.Messages)
            {
                if (msg.Role == "tool")
                {
                    messages.Add(new JsonObject
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = msg.ToolCallId ?? string.Empty,
                        ["name"] = msg.Name ?? string.Empty,
                        ["content"] = msg.Content
                    });
                }
                else if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
                {
                    var toolCallsArray = new JsonArray();
                    foreach (var tc in msg.ToolCalls)
                    {
                        toolCallsArray.Add(new JsonObject
                        {
                            ["id"] = tc.Id,
                            ["type"] = "function",
                            ["function"] = new JsonObject
                            {
                                ["name"] = tc.Name,
                                ["arguments"] = tc.ArgumentsJson
                            }
                        });
                    }
                    messages.Add(new JsonObject
                    {
                        ["role"] = "assistant",
                        ["tool_calls"] = toolCallsArray
                    });
                }
                else
                {
                    messages.Add(new JsonObject
                    {
                        ["role"] = msg.Role == "model" ? "assistant" : msg.Role,
                        ["content"] = msg.Content
                    });
                }
            }

            JsonObject? responseJson = null;
            bool success = false;

            while (!success && activeModelIndex < modelsToTry.Count)
            {
                var activeModel = modelsToTry[activeModelIndex];
                var requestBody = new JsonObject
                {
                    ["model"] = activeModel,
                    ["messages"] = messages.DeepClone(),
                    ["tools"] = request.ToolsDefinition.DeepClone(),
                    ["tool_choice"] = "auto"
                };

                _logger.LogInformation("Sending request to Hugging Face API (Model: {Model})...", activeModel);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint);
                httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                httpRequest.Content = JsonContent.Create(requestBody);

                try
                {
                    var httpResponse = await _httpClient.SendAsync(httpRequest, cancellationToken);
                    if (httpResponse.IsSuccessStatusCode)
                    {
                        responseJson = await httpResponse.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
                        if (responseJson != null)
                        {
                            success = true;
                        }
                    }
                    else
                    {
                        var errContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogWarning("Hugging Face API returned error for model {Model}: {Status} - {Content}. Trying fallback...", activeModel, httpResponse.StatusCode, errContent);
                        activeModelIndex++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to call Hugging Face with model {Model}. Trying fallback...", activeModel);
                    activeModelIndex++;
                }
            }

            if (!success || responseJson == null)
            {
                throw new Exception("All Hugging Face models failed to generate response.");
            }

            var choice = responseJson["choices"]?[0];
            var messageNode = choice?["message"];

            if (messageNode == null)
            {
                throw new Exception("Hugging Face API response did not contain message choices.");
            }

            var toolCallsNode = messageNode["tool_calls"]?.AsArray();
            if (toolCallsNode != null && toolCallsNode.Count > 0)
            {
                var toolCalls = new List<ToolCall>();
                foreach (var tc in toolCallsNode)
                {
                    if (tc == null) continue;
                    var id = tc["id"]?.ToString() ?? Guid.NewGuid().ToString("N");
                    var functionNode = tc["function"];
                    var name = functionNode?["name"]?.ToString() ?? "";
                    var args = functionNode?["arguments"]?.ToString() ?? "{}";

                    toolCalls.Add(new ToolCall
                    {
                        Id = id,
                        Name = name,
                        ArgumentsJson = args
                    });
                }

                return new ProviderChatResponse
                {
                    Content = string.Empty,
                    ToolCalls = toolCalls
                };
            }

            var content = messageNode["content"]?.ToString() ?? "";
            return new ProviderChatResponse
            {
                Content = content
            };
        }
    }
}
