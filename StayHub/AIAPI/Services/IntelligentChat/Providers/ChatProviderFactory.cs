using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AIAPI.Services.IntelligentChat.Providers
{
    public class ChatProviderFactory : IChatProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public ChatProviderFactory(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public IChatProvider GetProvider(string? providerName = null)
        {
            var targetProvider = providerName ?? _configuration["LLM:Provider"] ?? "Gemini";

            if (targetProvider.Equals("HuggingFace", StringComparison.OrdinalIgnoreCase))
            {
                return _serviceProvider.GetRequiredService<HuggingFaceChatProvider>();
            }

            return _serviceProvider.GetRequiredService<GeminiChatProvider>();
        }
    }
}
