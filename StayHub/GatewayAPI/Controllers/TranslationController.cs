using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GatewayAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranslationController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TranslationController> _logger;

        public TranslationController(
            IHttpClientFactory httpClientFactory, 
            IConfiguration configuration,
            ILogger<TranslationController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("translate")]
        public async Task<IActionResult> Translate([FromBody] TranslationRequest request)
        {
            if (request == null || request.Text == null || request.Text.Count == 0)
            {
                return BadRequest(new { error = "Text parameter is required." });
            }

            var apiKey = _configuration["DeepL:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = Environment.GetEnvironmentVariable("DEEPL_API_KEY");
            }
            if (string.IsNullOrEmpty(apiKey))
            {
                _logger.LogWarning("DeepL API key is not configured.");
                return StatusCode(500, new { error = "DeepL API key is not configured on the server." });
            }

            var isFree = apiKey.EndsWith(":fx");
            var url = isFree 
                ? "https://api-free.deepl.com/v2/translate" 
                : "https://api.deepl.com/v2/translate";

            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("DeepL-Auth-Key", apiKey);

                var requestBody = new
                {
                    text = request.Text,
                    target_lang = request.TargetLang ?? "VI",
                    source_lang = request.SourceLang ?? "EN",
                    tag_handling = request.TagHandling
                };

                var response = await client.PostAsJsonAsync(url, requestBody);
                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    _logger.LogError("DeepL API returned error status: {Status}. Details: {Details}", response.StatusCode, errorDetails);
                    return StatusCode((int)response.StatusCode, new { error = "DeepL API returned an error.", details = errorDetails });
                }

                var content = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(content);
                return Ok(jsonDoc.RootElement);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during translation proxy call.");
                return StatusCode(500, new { error = "An internal error occurred while processing translation.", details = ex.Message });
            }
        }
    }

    public class TranslationRequest
    {
        [JsonPropertyName("text")]
        public List<string> Text { get; set; } = new();

        [JsonPropertyName("target_lang")]
        public string? TargetLang { get; set; }

        [JsonPropertyName("source_lang")]
        public string? SourceLang { get; set; }

        [JsonPropertyName("tag_handling")]
        public string? TagHandling { get; set; }
    }
}
