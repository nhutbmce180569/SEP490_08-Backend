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
    public class GeminiChatProvider : IChatProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GeminiChatProvider> _logger;

        public GeminiChatProvider(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiChatProvider> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ProviderChatResponse> ChatAsync(ProviderChatRequest request, string systemInstruction, CancellationToken cancellationToken)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var modelName = _configuration["Gemini:ModelName"] ?? "gemini-2.5-flash-lite";

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY_HERE")
            {
                apiKey = Environment.GetEnvironmentVariable("Gemini__ApiKey") ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? apiKey;
            }

            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_GEMINI_API_KEY_HERE")
            {
                throw new InvalidOperationException("Gemini API key is not configured.");
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

            var contents = new JsonArray();

            foreach (var msg in request.Messages)
            {
                var role = msg.Role == "assistant" ? "model" : msg.Role;
                var parts = new JsonArray();

                if (msg.Role == "tool")
                {
                    parts.Add(new JsonObject
                    {
                        ["functionResponse"] = new JsonObject
                        {
                            ["name"] = msg.Name ?? "",
                            ["response"] = JsonNode.Parse(msg.Content)
                        }
                    });
                    // For Gemini, tool response role must be "user"
                    contents.Add(new JsonObject
                    {
                        ["role"] = "user",
                        ["parts"] = parts
                    });
                }
                else if (msg.ToolCalls != null && msg.ToolCalls.Count > 0)
                {
                    foreach (var tc in msg.ToolCalls)
                    {
                        parts.Add(new JsonObject
                        {
                            ["functionCall"] = new JsonObject
                            {
                                ["name"] = tc.Name,
                                ["args"] = JsonNode.Parse(tc.ArgumentsJson)
                            }
                        });
                    }
                    contents.Add(new JsonObject
                    {
                        ["role"] = role,
                        ["parts"] = parts
                    });
                }
                else
                {
                    parts.Add(new JsonObject
                    {
                        ["text"] = msg.Content
                    });
                    contents.Add(new JsonObject
                    {
                        ["role"] = role,
                        ["parts"] = parts
                    });
                }
            }

            var systemInstructionNode = new JsonObject
            {
                ["parts"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["text"] = systemInstruction
                    }
                }
            };

            var requestBody = new JsonObject
            {
                ["contents"] = contents,
                ["systemInstruction"] = systemInstructionNode,
                ["tools"] = request.ToolsDefinition.DeepClone(),
                ["toolConfig"] = new JsonObject
                {
                    ["functionCallingConfig"] = new JsonObject
                    {
                        ["mode"] = "AUTO"
                    }
                }
            };

            var httpResponse = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            if (!httpResponse.IsSuccessStatusCode)
            {
                var errContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Gemini API returned error code {Status}: {Content}", httpResponse.StatusCode, errContent);
                throw new Exception($"Gemini API error: {httpResponse.StatusCode}");
            }

            var responseJson = await httpResponse.Content.ReadFromJsonAsync<JsonObject>(cancellationToken: cancellationToken);
            if (responseJson == null)
            {
                throw new Exception("Received empty response from Gemini API.");
            }

            var candidate = responseJson["candidates"]?[0];
            if (candidate == null)
            {
                throw new Exception("Gemini API did not return any candidates.");
            }

            var contentObj = candidate["content"];
            var responseParts = contentObj?["parts"]?.AsArray();

            if (responseParts == null || responseParts.Count == 0)
            {
                throw new Exception("Gemini API candidate content has no parts.");
            }

            var functionCallNode = responseParts.FirstOrDefault(p => p?["functionCall"] != null);
            if (functionCallNode != null)
            {
                var functionCall = functionCallNode["functionCall"];
                var name = functionCall?["name"]?.ToString() ?? "";
                var args = functionCall?["args"]?.ToString() ?? "{}";

                return new ProviderChatResponse
                {
                    Content = string.Empty,
                    ToolCalls = new List<ToolCall>
                    {
                        new ToolCall
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            Name = name,
                            ArgumentsJson = args
                        }
                    }
                };
            }

            var textNode = responseParts.FirstOrDefault(p => p?["text"] != null);
            var text = textNode?["text"]?.ToString() ?? "";

            return new ProviderChatResponse
            {
                Content = text
            };
        }
    }
}
