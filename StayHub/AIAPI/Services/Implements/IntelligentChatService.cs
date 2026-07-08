using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AIAPI.DTOs;
using AIAPI.Localization;
using AIAPI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIAPI.Services.Implements;

public class IntelligentChatService : IIntelligentChatService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly IPersonalizedTourRecommendationService _personalizedService;
    private readonly ITourSemanticSearchService _searchService;
    private readonly IWeatherService _weatherService;
    private readonly ILogger<IntelligentChatService> _logger;
    private readonly IAiCultureAccessor _cultureAccessor;

    public IntelligentChatService(
        IConfiguration configuration,
        HttpClient httpClient,
        IPersonalizedTourRecommendationService personalizedService,
        ITourSemanticSearchService searchService,
        IWeatherService weatherService,
        ILogger<IntelligentChatService> logger,
        IAiCultureAccessor cultureAccessor)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _personalizedService = personalizedService;
        _searchService = searchService;
        _weatherService = weatherService;
        _logger = logger;
        _cultureAccessor = cultureAccessor;
    }

    public async Task<IntelligentChatResponseDTO> ChatAsync(
        IntelligentChatRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _configuration["Gemini:ApiKey"];
        var modelName = _configuration["Gemini:ModelName"] ?? "gemini-1.5-flash";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured.");
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

        // 1. Build initial contents history
        var contents = new JsonArray();

        // Add history if present
        if (request.History != null && request.History.Count > 0)
        {
            foreach (var h in request.History)
            {
                var role = h.Role == "assistant" ? "model" : h.Role;
                contents.Add(new JsonObject
                {
                    ["role"] = role,
                    ["parts"] = new JsonArray
                    {
                        new JsonObject { ["text"] = h.Content }
                    }
                });
            }
        }

        // Add current user message
        contents.Add(new JsonObject
        {
            ["role"] = "user",
            ["parts"] = new JsonArray
            {
                new JsonObject { ["text"] = request.Message }
            }
        });

        // 2. Define tools
        var tools = GetToolsDefinition();

        // 3. Define system instruction
        var promptText = _cultureAccessor.IsVietnamese
            ? "Bạn là trợ lý du lịch thông minh StayHub AI Agent. " +
              "Nhiệm vụ của bạn là trò chuyện tự nhiên, tư vấn du lịch và giúp người dùng tìm kiếm, đề xuất các tour phù hợp. " +
              "Khi người dùng muốn lên kế hoạch hoặc tìm kiếm tour, bạn hãy chủ động hỏi han hoặc dùng các Tool có sẵn để gợi ý. " +
              "Các Tool có sẵn bao gồm: " +
              "1. recommend_tours_from_profile: Dùng khi người dùng muốn nhận gợi ý tour cá nhân hóa và bạn đã biết các thông tin cơ bản: loại bạn đồng hành (cá nhân/gia đình/cặp đôi/nhóm), ngày đi dự kiến, các sở thích du lịch và thành phố muốn đi (bắt buộc phải có các thông tin này). Nếu thiếu, hãy lịch sự hỏi thăm người dùng thay vì gọi hàm với dữ liệu giả. " +
              "2. search_tours: Dùng khi người dùng tìm kiếm tour cụ thể bằng từ khóa (ví dụ: tour trekking, tour vịnh hạ long). " +
              "3. search_tourism_insights: Dùng khi người dùng hỏi về ẩm thực, đặc sản, văn hóa, danh lam thắng cảnh ở một thành phố nào đó. " +
              "4. get_weather_forecast: Dùng khi người dùng hỏi về thời tiết của một thành phố cụ thể. " +
              $"Hôm nay là ngày {DateTime.Today:yyyy-MM-dd}. Nếu người dùng hỏi về các khoảng thời gian tương đối như 'tuần tới', 'hôm nay', 'ngày mai', hãy tự tính toán ngày cụ thể dựa trên hôm nay để gọi Tool. " +
              "Hãy phản hồi bằng tiếng Việt thân thiện, lịch sự, chuyên nghiệp."
            : "You are a smart travel assistant named StayHub AI Agent. " +
              "Your task is to chat naturally, consult on travel, and help users search and recommend suitable tours. " +
              "When the user wants to plan a trip or search for tours, proactively ask them or use the available tools to suggest. " +
              "The available tools include: " +
              "1. recommend_tours_from_profile: Use when the user wants to get personalized tour recommendations and you already know basic information: companion type (solo/family/couple/group), preferred start date, travel interests, and preferred city (these are required). If any are missing, politely ask the user instead of calling the function with fake data. " +
              "2. search_tours: Use when the user searches for a specific tour by keywords (e.g., trekking tour, halong bay tour). " +
              "3. search_tourism_insights: Use when the user asks about local food, specialties, culture, or attractions in a city. " +
              "4. get_weather_forecast: Use when the user asks about the weather of a specific city. " +
              $"Today is {DateTime.Today:yyyy-MM-dd}. If the user asks about relative times like 'next week', 'today', 'tomorrow', calculate the specific date based on today to call the Tool. " +
              "Please respond in a friendly, polite, and professional manner in English.";

        var systemInstruction = new JsonObject
        {
            ["parts"] = new JsonArray
            {
                new JsonObject
                {
                    ["text"] = promptText
                }
            }
        };

        // Cache results of local service calls
        var recommendedTours = new List<TourRecommendationItemDTO>();
        WeatherAdviceDTO? weatherAdvice = null;
        var culturalFacts = new List<CulturalFactDTO>();
        var tourismInsights = new List<TourismInsightDTO>();

        int maxTurns = 5;
        int currentTurn = 0;
        string finalReply = "";

        while (currentTurn < maxTurns)
        {
            currentTurn++;

            var requestBody = new JsonObject
            {
                ["contents"] = contents.DeepClone(),
                ["systemInstruction"] = systemInstruction.DeepClone(),
                ["tools"] = tools.DeepClone(),
                ["toolConfig"] = new JsonObject
                {
                    ["functionCallingConfig"] = new JsonObject
                    {
                        ["mode"] = "AUTO"
                    }
                }
            };

            _logger.LogInformation("Sending request to Gemini API (Turn {Turn})...", currentTurn);

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
            var parts = contentObj?["parts"]?.AsArray();

            if (parts == null || parts.Count == 0)
            {
                throw new Exception("Gemini API candidate content has no parts.");
            }

            // Check if model wants to call a function
            var functionCallNode = parts.FirstOrDefault(p => p?["functionCall"] != null);

            if (functionCallNode != null)
            {
                var functionCall = functionCallNode["functionCall"];
                var functionName = functionCall?["name"]?.ToString() ?? "";
                var functionArgs = functionCall?["args"]?.AsObject();

                _logger.LogInformation("Gemini requested function call: {FunctionName}", functionName);

                // Add the model's turn (containing function call) to history
                if (contentObj != null)
                {
                    contents.Add(contentObj.DeepClone());
                }

                // Execute local function
                JsonObject toolResponseData;

                try
                {
                    if (functionName == "recommend_tours_from_profile")
                    {
                        var (toolResponse, weather) = await HandleRecommendToursTool(functionArgs, recommendedTours, culturalFacts, cancellationToken);
                        toolResponseData = toolResponse;
                        weatherAdvice = weather;
                    }
                    else if (functionName == "search_tours")
                    {
                        toolResponseData = await HandleSearchToursTool(functionArgs, recommendedTours, cancellationToken);
                    }
                    else if (functionName == "search_tourism_insights")
                    {
                        toolResponseData = await HandleSearchTourismInsightsTool(functionArgs, tourismInsights, cancellationToken);
                    }
                    else if (functionName == "get_weather_forecast")
                    {
                        var city = functionArgs?["city"]?.ToString() ?? "";
                        var startDateStr = functionArgs?["startDate"]?.ToString() ?? DateTime.Today.ToString("yyyy-MM-dd");
                        var endDateStr = functionArgs?["endDate"]?.ToString();
                        
                        DateTime startDate = DateTime.TryParse(startDateStr, out var d1) ? d1 : DateTime.Today;
                        DateTime? endDate = DateTime.TryParse(endDateStr, out var d2) ? d2 : null;

                        _logger.LogInformation("Calling local IWeatherService for city {City}...", city);
                        var advice = await _weatherService.GetTravelWeatherAdviceAsync(city, startDate, endDate, cancellationToken: cancellationToken);
                        weatherAdvice = advice;
                        
                        if (advice != null)
                        {
                            toolResponseData = new JsonObject
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
                            toolResponseData = new JsonObject { ["error"] = "No weather data found." };
                        }
                    }
                    else
                    {
                        toolResponseData = new JsonObject { ["error"] = $"Unknown function: {functionName}" };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing local tool {FunctionName}", functionName);
                    toolResponseData = new JsonObject { ["error"] = ex.Message };
                }

                // Add function response to history
                contents.Add(new JsonObject
                {
                    ["role"] = "user",
                    ["parts"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["functionResponse"] = new JsonObject
                            {
                                ["name"] = functionName,
                                ["response"] = toolResponseData
                            }
                        }
                    }
                });

                // Loop back to send tool response to Gemini
                continue;
            }

            // No function call, get final text response
            var textPart = parts.FirstOrDefault(p => p?["text"] != null);
            if (textPart != null)
            {
                finalReply = textPart["text"]?.ToString() ?? "";
                break;
            }

            break;
        }

        // 4. Generate suggested questions dynamically (optional fallback)
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

    private async Task<(JsonObject ToolResponse, WeatherAdviceDTO? Weather)> HandleRecommendToursTool(
        JsonObject? args,
        List<TourRecommendationItemDTO> recommendedTours,
        List<CulturalFactDTO> culturalFacts,
        CancellationToken cancellationToken)
    {
        if (args == null)
        {
            return (new JsonObject { ["error"] = "Arguments are missing." }, null);
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

        // Store full details for rich frontend UI
        if (result.RecommendedTours != null)
        {
            recommendedTours.AddRange(result.RecommendedTours);
        }
        if (result.NearbyScheduleTours != null)
        {
            recommendedTours.AddRange(result.NearbyScheduleTours);
        }

        if (result.CulturalFacts != null)
        {
            culturalFacts.AddRange(result.CulturalFacts);
        }

        // Return simplified response for LLM synthesis
        var simplifiedTours = new JsonArray();
        foreach (var t in recommendedTours.Take(3))
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

        var toolResponse = new JsonObject
        {
            ["summary"] = result.Summary,
            ["weatherSummary"] = result.WeatherAdvice?.Summary ?? "",
            ["impactOnTours"] = result.WeatherAdvice?.ImpactOnTours ?? "",
            ["recommendedTours"] = simplifiedTours
        };

        return (toolResponse, result.WeatherAdvice);
    }

    private async Task<JsonObject> HandleSearchToursTool(
        JsonObject? args,
        List<TourRecommendationItemDTO> recommendedTours,
        CancellationToken cancellationToken)
    {
        if (args == null)
        {
            return new JsonObject { ["error"] = "Arguments are missing." };
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

        var simplifiedTours = new JsonArray();
        if (searchResults != null)
        {
            foreach (var r in searchResults)
            {
                recommendedTours.Add(r);
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

        return new JsonObject
        {
            ["resultsCount"] = searchResults?.Count ?? 0,
            ["tours"] = simplifiedTours
        };
    }

    private async Task<JsonObject> HandleSearchTourismInsightsTool(
        JsonObject? args,
        List<TourismInsightDTO> tourismInsights,
        CancellationToken cancellationToken)
    {
        if (args == null)
        {
            return new JsonObject { ["error"] = "Arguments are missing." };
        }

        var query = args["query"]?.ToString() ?? "";
        var city = args["city"]?.ToString();

        _logger.LogInformation("Calling local TourSemanticSearchService.SearchTourismAsync for '{Query}'...", query);
        var insights = await _searchService.SearchTourismAsync(query, city, 4, cancellationToken);

        var simplifiedInsights = new JsonArray();
        if (insights != null)
        {
            foreach (var ins in insights)
            {
                tourismInsights.Add(ins);
                simplifiedInsights.Add(new JsonObject
                {
                    ["name"] = ins.Name,
                    ["type"] = ins.Type,
                    ["description"] = ins.Description
                });
            }
        }

        return new JsonObject
        {
            ["insightsCount"] = insights?.Count ?? 0,
            ["insights"] = simplifiedInsights
        };
    }

    private JsonArray GetToolsDefinition()
    {
        return new JsonArray
        {
            new JsonObject
            {
                ["functionDeclarations"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["name"] = "recommend_tours_from_profile",
                        ["description"] = "Gợi ý tour du lịch cá nhân hóa dựa trên các sở thích, ngân sách, số lượng người, ngày đi và điểm đến mong muốn.",
                        ["parameters"] = new JsonObject
                        {
                            ["type"] = "OBJECT",
                            ["properties"] = new JsonObject
                            {
                                ["companionType"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["enum"] = new JsonArray { "solo", "family", "couple", "group" },
                                    ["description"] = "Loại bạn đồng hành: solo (đi một mình), family (gia đình), couple (cặp đôi) hoặc group (nhóm bạn)."
                                },
                                ["preferredStartDate"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "Ngày bắt đầu dự kiến (định dạng YYYY-MM-DD)."
                                },
                                ["preferredEndDate"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "Ngày kết thúc dự kiến (định dạng YYYY-MM-DD)."
                                },
                                ["maxBudgetPerPerson"] = new JsonObject
                                {
                                    ["type"] = "INTEGER",
                                    ["description"] = "Ngân sách tối đa cho một người (VND)."
                                },
                                ["travelPace"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["enum"] = new JsonArray { "relaxed", "moderate", "packed" },
                                    ["description"] = "Nhịp độ chuyến đi: relaxed (thư thả), moderate (vừa phải), packed (dày đặc)."
                                },
                                ["adultCount"] = new JsonObject
                                {
                                    ["type"] = "INTEGER",
                                    ["description"] = "Số lượng người lớn."
                                },
                                ["childrenCount"] = new JsonObject
                                {
                                    ["type"] = "INTEGER",
                                    ["description"] = "Số lượng trẻ em."
                                },
                                ["elderlyCount"] = new JsonObject
                                {
                                    ["type"] = "INTEGER",
                                    ["description"] = "Số lượng người cao tuổi."
                                },
                                ["travelInterests"] = new JsonObject
                                {
                                    ["type"] = "ARRAY",
                                    ["items"] = new JsonObject { ["type"] = "STRING" },
                                    ["description"] = "Danh sách các sở thích du lịch (ví dụ: ẩm thực, lịch sử, biển, núi, nghỉ dưỡng, khám phá)."
                                },
                                ["preferredCity"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "Thành phố muốn đi du lịch ở Việt Nam (ví dụ: Đà Lạt, Nha Trang, Đà Nẵng, Huế, Sa Pa)."
                                },
                                ["preferredCountry"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "Quốc gia muốn đi du lịch (mặc định: Việt Nam)."
                                }
                            },
                            ["required"] = new JsonArray { "companionType", "preferredStartDate", "travelInterests", "preferredCity" }
                        }
                    },
                    new JsonObject
                    {
                        ["name"] = "search_tours",
                        ["description"] = "Tìm kiếm các tour du lịch dựa trên từ khóa tìm kiếm tự nhiên.",
                        ["parameters"] = new JsonObject
                        {
                            ["type"] = "OBJECT",
                            ["properties"] = new JsonObject
                            {
                                ["query"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "Từ khóa tìm kiếm (ví dụ: tour trekking, tour du thuyền)."
                                },
                                ["city"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "Thành phố lọc kết quả (ví dụ: Đà Lạt, Nha Trang)."
                                },
                                ["minPrice"] = new JsonObject
                                {
                                    ["type"] = "INTEGER",
                                    ["description"] = "Giá tối thiểu (VND)."
                                },
                                ["maxPrice"] = new JsonObject
                                {
                                    ["type"] = "INTEGER",
                                    ["description"] = "Giá tối đa (VND)."
                                },
                                ["durationDays"] = new JsonObject
                                {
                                    ["type"] = "INTEGER",
                                    ["description"] = "Số ngày của tour."
                                }
                            },
                            ["required"] = new JsonArray { "query" }
                        }
                    },
                    new JsonObject
                    {
                        ["name"] = "search_tourism_insights",
                        ["description"] = "Tìm kiếm thông tin ẩm thực, văn hóa địa phương, đặc sản, danh lam thắng cảnh ở một thành phố nào đó.",
                        ["parameters"] = new JsonObject
                        {
                            ["type"] = "OBJECT",
                            ["properties"] = new JsonObject
                            {
                                ["query"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "Từ khóa cần tìm hiểu (ví dụ: đặc sản Đà Lạt, văn hóa ẩm thực Huế)."
                                },
                                ["city"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "Tên thành phố cần tìm hiểu thông tin."
                                }
                            },
                            ["required"] = new JsonArray { "query" }
                        }
                    },
                    new JsonObject
                    {
                        ["name"] = "get_weather_forecast",
                        ["description"] = "Xem thông tin dự báo thời tiết của một thành phố tại Việt Nam.",
                        ["parameters"] = new JsonObject
                        {
                            ["type"] = "OBJECT",
                            ["properties"] = new JsonObject
                            {
                                ["city"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "Tên thành phố cần xem thời tiết (ví dụ: Đà Lạt, Nha Trang, Đà Nẵng)."
                                },
                                ["startDate"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "Ngày bắt đầu cần dự báo (định dạng YYYY-MM-DD)."
                                },
                                ["endDate"] = new JsonObject
                                {
                                    ["type"] = "STRING",
                                    ["description"] = "Ngày kết thúc dự báo (định dạng YYYY-MM-DD, tùy chọn)."
                                }
                            },
                            ["required"] = new JsonArray { "city" }
                        }
                    }
                }
            }
        };
    }
}
