using System.Threading;
using System.Threading.Tasks;
using AIAPI.Services.IntelligentChat.Models;

namespace AIAPI.Services.IntelligentChat.Providers
{
    public interface IChatProvider
    {
        Task<ProviderChatResponse> ChatAsync(ProviderChatRequest request, string systemInstruction, CancellationToken cancellationToken);
    }
}
