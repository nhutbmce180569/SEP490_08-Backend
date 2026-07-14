using System.Collections.Generic;

namespace AIAPI.Services.IntelligentChat.Models
{
    public class ProviderChatResponse
    {
        public string Content { get; set; } = string.Empty;
        public List<ToolCall>? ToolCalls { get; set; }
        public bool HasToolCalls => ToolCalls != null && ToolCalls.Count > 0;
    }
}
