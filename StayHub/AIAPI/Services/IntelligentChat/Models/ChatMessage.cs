using System.Collections.Generic;

namespace AIAPI.Services.IntelligentChat.Models
{
    public class ToolCall
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ArgumentsJson { get; set; } = string.Empty;
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty; // "user", "model", "system", "tool"
        public string Content { get; set; } = string.Empty;
        public string? ToolCallId { get; set; }
        public string? Name { get; set; }
        public List<ToolCall>? ToolCalls { get; set; }
    }
}
