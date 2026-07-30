using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AIAPI.Services.IntelligentChat.Models;

namespace AIAPI.Services.IntelligentChat.Tools
{
    public interface IToolExecutor
    {
        string FunctionName { get; }
        Task<ToolExecutionResult> ExecuteAsync(JsonObject? args, CancellationToken cancellationToken);
    }
}
