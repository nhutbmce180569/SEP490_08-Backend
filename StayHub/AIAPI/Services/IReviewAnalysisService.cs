using System.Threading.Tasks;
using AIAPI.DTOs;

namespace AIAPI.Services;

public interface IReviewAnalysisService
{
    Task<ReviewAnalysisResponseDTO> AnalyzeReviewAsync(ReviewAnalysisRequestDTO request);
}
