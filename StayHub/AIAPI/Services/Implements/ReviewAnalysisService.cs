using System.Text;
using System.Text.Json;
using AIAPI.DTOs;

namespace AIAPI.Services.Implements;

public class ReviewAnalysisService : IReviewAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReviewAnalysisService> _logger;

    public ReviewAnalysisService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<ReviewAnalysisService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ReviewAnalysisResponseDTO> AnalyzeReviewAsync(ReviewAnalysisRequestDTO request)
    {
        try
        {
            string prompt = $@"
Bạn là một chuyên gia phân tích cảm xúc đánh giá tour du lịch.
Nhiệm vụ của bạn là đọc nội dung đánh giá (Review Text) và số sao (Star Rating) để phân loại cảm xúc thành đúng 1 trong 3 loại: 'Good', 'Neutral' hoặc 'Bad'.

Quy tắc quan trọng:
- Phải xem xét ngữ cảnh mỉa mai (Sarcasm). Ví dụ: Đánh 5 sao nhưng bình luận là 'Dịch vụ quá tệ, sẽ không bao giờ quay lại' thì cảm xúc phải là 'Bad'.
- Đánh giá 1-2 sao thường là 'Bad'. 3 sao là 'Neutral'. 4-5 sao thường là 'Good'.
- TUY NHIÊN, luôn ưu tiên nội dung chữ (Review Text) hơn số sao nếu có sự mâu thuẫn.

Dữ liệu đầu vào:
- Số sao: {request.StarRating}
- Bình luận: ""{request.ReviewText}""

BẮT BUỘC TRẢ VỀ DỮ LIỆU JSON ĐÚNG SCHEMA SAU ĐÂY VÀ CHỈ TRẢ VỀ JSON, KHÔNG THÊM BẤT KỲ VĂN BẢN NÀO KHÁC:
{{
  ""sentiment"": ""Good/Neutral/Bad"",
  ""confidence"": 0.9,
  ""reason"": ""Giải thích ngắn gọn bằng tiếng Việt""
}}";

            string provider = _configuration["LLM:Provider"] ?? "Gemini";
            string textResponse = string.Empty;

            if (provider.Equals("HuggingFace", StringComparison.OrdinalIgnoreCase))
            {
                textResponse = await CallHuggingFaceApiAsync(prompt);
            }
            else
            {
                textResponse = await CallGeminiApiAsync(prompt);
            }

            if (!string.IsNullOrWhiteSpace(textResponse))
            {
                // Làm sạch textResponse đề phòng LLM trả về markdown ```json ... ```
                textResponse = textResponse.Trim();
                if (textResponse.StartsWith("```json"))
                {
                    textResponse = textResponse.Substring(7);
                }
                if (textResponse.StartsWith("```"))
                {
                    textResponse = textResponse.Substring(3);
                }
                if (textResponse.EndsWith("```"))
                {
                    textResponse = textResponse.Substring(0, textResponse.Length - 3);
                }
                textResponse = textResponse.Trim();

                var result = JsonSerializer.Deserialize<ReviewAnalysisResponseDTO>(textResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result != null)
                {
                    return result;
                }
            }

            throw new Exception("Không thể trích xuất text hợp lệ từ kết quả của LLM.");
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "Lỗi giao tiếp với API LLM.");
            throw new Exception("Lỗi mạng khi kết nối tới dịch vụ AI: " + httpEx.Message, httpEx);
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError(jsonEx, "Lỗi khi parse dữ liệu JSON từ LLM.");
            throw new Exception("Dịch vụ AI trả về dữ liệu không đúng định dạng JSON.", jsonEx);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi hệ thống khi phân tích đánh giá tour.");
            throw;
        }
    }

    private async Task<string> CallGeminiApiAsync(string prompt)
    {
        string apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException("Gemini API Key is missing in configuration.");
        }

        string modelName = _configuration["Gemini:ModelName"] ?? "gemini-1.5-flash";
        string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                responseMimeType = "application/json"
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(apiUrl, content);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Gemini API Error ({response.StatusCode}): {errorBody}");
        }

        var responseString = await response.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(responseString);
        var candidates = jsonDocument.RootElement.GetProperty("candidates");
        
        if (candidates.GetArrayLength() > 0)
        {
            return candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        }
        return "";
    }

    private async Task<string> CallHuggingFaceApiAsync(string prompt)
    {
        string apiKey = _configuration["LLM:HuggingFace:ApiKey"];
        string endpoint = _configuration["LLM:HuggingFace:Endpoint"];
        string modelName = _configuration["LLM:HuggingFace:ModelName"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(endpoint))
        {
            throw new InvalidOperationException("HuggingFace configuration is missing.");
        }

        var requestBody = new
        {
            model = modelName,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.1,
            max_tokens = 500
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
        requestMessage.Headers.Add("Authorization", $"Bearer {apiKey}");
        requestMessage.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(requestMessage);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"HuggingFace API Error ({response.StatusCode}): {errorBody}");
        }

        var responseString = await response.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(responseString);
        var choices = jsonDocument.RootElement.GetProperty("choices");
        
        if (choices.GetArrayLength() > 0)
        {
            return choices[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        }
        return "";
    }
}
