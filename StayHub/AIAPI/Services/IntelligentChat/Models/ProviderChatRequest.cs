using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AIAPI.Services.IntelligentChat.Models
{
    public class ProviderChatRequest
    {
        public List<ChatMessage> Messages { get; set; } = new();
        public JsonArray ToolsDefinition { get; set; } = new();
    }
}
