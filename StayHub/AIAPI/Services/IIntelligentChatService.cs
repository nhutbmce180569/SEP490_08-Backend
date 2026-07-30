using AIAPI.DTOs;

namespace AIAPI.Services;

public interface IIntelligentChatService
{
    Task<IntelligentChatResponseDTO> ChatAsync(IntelligentChatRequestDTO request, CancellationToken cancellationToken = default);
}
