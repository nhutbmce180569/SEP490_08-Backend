using AIAPI.DTOs;
using AIAPI.Models.Knowledge;
using AIAPI.Recommender;
using AIAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIAPI.Controllers;

[Route("api/ai/knowledge")]
[ApiController]
public class KnowledgeController : ControllerBase
{
    private readonly ICulturalKnowledgeService _knowledgeService;
    private readonly ICatalogStore _catalogStore;

    public KnowledgeController(ICulturalKnowledgeService knowledgeService, ICatalogStore catalogStore)
    {
        _knowledgeService = knowledgeService;
        _catalogStore = catalogStore;
    }

    [AllowAnonymous]
    [HttpGet("corpus-stats")]
    public ActionResult<RagCorpusStatsDTO> GetCorpusStats()
    {
        var stats = _knowledgeService.GetCorpusStats(_catalogStore.TourismItems.Count);
        return Ok(stats);
    }

    [AllowAnonymous]
    [HttpGet("rag/search")]
    public ActionResult<IReadOnlyList<RagRetrievalResult>> SearchRag(
        [FromQuery] string query,
        [FromQuery] string? city,
        [FromQuery] string? interests,
        [FromQuery] bool foreignVisitor = false,
        [FromQuery] bool elderly = false,
        [FromQuery] bool children = false,
        [FromQuery] int top = 8)
    {
        var interestList = string.IsNullOrWhiteSpace(interests)
            ? null
            : interests.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var results = _knowledgeService.SearchRag(
            query, city, interestList, foreignVisitor, elderly, children, top);

        return Ok(results);
    }

    [AllowAnonymous]
    [HttpGet("facts")]
    public async Task<ActionResult<IReadOnlyList<CulturalFactDTO>>> GetFacts(
        [FromQuery] string? city,
        [FromQuery] bool foreignVisitor = false,
        [FromQuery] bool elderly = false,
        [FromQuery] bool children = false,
        [FromQuery] string? interests = null,
        CancellationToken cancellationToken = default)
    {
        var interestList = string.IsNullOrWhiteSpace(interests)
            ? null
            : interests.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var facts = await _knowledgeService.GetFactsAsync(
            city, foreignVisitor, elderly, children, interestList, cancellationToken);

        return Ok(facts.Select(f => new CulturalFactDTO
        {
            Fact = f.Fact,
            SourceName = f.SourceName,
            SourceUrl = f.SourceUrl,
            AuthorityLevel = f.AuthorityLevel,
            Provider = f.Provider,
            City = f.City
        }).ToList());
    }
}
