using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SocialAPI.Helpers;

public interface IContentModerator
{
    Task<string> ModerateTextAsync(string content);
}

public class ContentModerator : IContentModerator
{
    private readonly HashSet<string> _blacklist = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _enableCloudModeration;
    private readonly string? _openAiApiKey;
    private readonly IHttpClientFactory _httpClientFactory;

    public ContentModerator(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _enableCloudModeration = configuration.GetValue<bool>("EnableCloudModeration");
        _openAiApiKey = configuration.GetValue<string>("OpenAiApiKey");

        // Load local blacklist
        LoadLocalBlacklist();
    }

    private void LoadLocalBlacklist()
    {
        // Default list in memory
        var defaultWords = new[] { "đm", "đmm", "đéo", "đệt", "vcl", "buồi", "cặc", "lồn", "chịch", "chém giết" };
        foreach (var word in defaultWords)
        {
            _blacklist.Add(word);
        }

        try
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "blacklist.txt");
            if (!File.Exists(filePath))
            {
                // Try parent folder context for development
                filePath = Path.Combine(Directory.GetCurrentDirectory(), "blacklist.txt");
            }

            if (File.Exists(filePath))
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var word = line.Trim();
                    if (!string.IsNullOrEmpty(word))
                    {
                        _blacklist.Add(word);
                    }
                }
            }
        }
        catch
        {
            // Fallback silently to default memory words
        }
    }

    public async Task<string> ModerateTextAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return "Approved";
        }

        // 1. Local Blacklist Check (Lớp 1: Lọc thô cục bộ)
        foreach (var word in _blacklist)
        {
            if (content.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return "Rejected"; // Chặn ngay lập tức
            }
        }

        // 2. Cloud AI Moderation (Lớp 2: OpenAI Moderation API nếu cấu hình bật và có API Key)
        if (_enableCloudModeration && !string.IsNullOrEmpty(_openAiApiKey))
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _openAiApiKey);
                
                var request = new OpenAIModerationRequest { Input = content };
                var response = await client.PostAsJsonAsync("https://api.openai.com/v1/moderations", request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OpenAIModerationResponse>();
                    var isFlagged = result?.Results?.FirstOrDefault()?.Flagged ?? false;
                    if (isFlagged)
                    {
                        return "Rejected";
                    }
                }
            }
            catch
            {
                // Fallback to Approved if cloud is unavailable to ensure uptime for demo
            }
        }

        return "Approved";
    }

    private class OpenAIModerationRequest
    {
        [JsonPropertyName("input")]
        public string Input { get; set; } = null!;
    }

    private class OpenAIModerationResponse
    {
        [JsonPropertyName("results")]
        public List<ModerationResult>? Results { get; set; }
    }

    private class ModerationResult
    {
        [JsonPropertyName("flagged")]
        public bool Flagged { get; set; }
    }
}
