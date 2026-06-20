using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Localization;
using AIAPI.ML;
using AIAPI.Models.Knowledge;
using AIAPI.Services;

namespace AIAPI.Services.Implements;

public class TourAssistantService : ITourAssistantService
{
    private readonly IMlModelRegistry _modelRegistry;
    private readonly ICatalogStore _catalogStore;
    private readonly ITourSemanticSearchService _searchService;
    private readonly IPersonalizedTourRecommendationService _personalizedService;
    private readonly ICulturalKnowledgeService _culturalKnowledge;
    private readonly ISystemKnowledgeIndex _systemKnowledge;
    private readonly IKnowledgeLocalizationService _knowledgeLocalization;
    private readonly QueryEntityExtractor _entityExtractor;
    private readonly IAiLocalizedCopy _text;

    public TourAssistantService(
        IMlModelRegistry modelRegistry,
        ICatalogStore catalogStore,
        ITourSemanticSearchService searchService,
        IPersonalizedTourRecommendationService personalizedService,
        ICulturalKnowledgeService culturalKnowledge,
        ISystemKnowledgeIndex systemKnowledge,
        IKnowledgeLocalizationService knowledgeLocalization,
        QueryEntityExtractor entityExtractor,
        IAiLocalizedCopy text)
    {
        _modelRegistry = modelRegistry;
        _catalogStore = catalogStore;
        _searchService = searchService;
        _personalizedService = personalizedService;
        _culturalKnowledge = culturalKnowledge;
        _systemKnowledge = systemKnowledge;
        _knowledgeLocalization = knowledgeLocalization;
        _entityExtractor = entityExtractor;
        _text = text;
    }

    public async Task<ChatResponseDTO> ChatAsync(string message, string? sessionId, CancellationToken cancellationToken = default)
    {
        if (!_catalogStore.IsReady || !_modelRegistry.Status.IsReady)
        {
            throw new InvalidOperationException("AI assistant is warming up. Please retry in a few seconds.");
        }

        var session = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId;
        var (mlIntent, confidence) = _modelRegistry.PredictIntent(message);
        var intent = ChatIntentResolver.Resolve(message, mlIntent, confidence);
        var parsed = _entityExtractor.Extract(message, _catalogStore);

        var response = new ChatResponseDTO
        {
            SessionId = session,
            Intent = intent,
            IntentConfidence = confidence,
            ParsedQuery = parsed,
            UsedPersonalization = false
        };

        switch (intent)
        {
            case TourIntents.Greeting:
                response.Reply = _text.ChatGreeting;
                response.SuggestedQuestions = _text.ChatDefaultSuggestions.ToList();
                break;

            case TourIntents.AskSystem:
                HandleSystemQuestion(response, message);
                break;

            case TourIntents.AskCulture:
            case TourIntents.AskDestination:
                await HandleCultureQuestionAsync(response, message, parsed, cancellationToken);
                break;

            case TourIntents.AskBudget:
            case TourIntents.SearchTour:
                await HandleTourSearchAsync(response, message, parsed, cancellationToken);
                break;

            case TourIntents.RecommendTour:
            default:
                if (TryAnswerFromSystemKnowledge(response, message))
                {
                    break;
                }

                await HandleRecommendationAsync(response, message, parsed, cancellationToken);
                break;
        }

        return response;
    }

    private void HandleSystemQuestion(ChatResponseDTO response, string message)
    {
        var hits = _systemKnowledge.Retrieve(message, topK: 4);
        if (hits.Count == 0)
        {
            response.Reply = _text.ChatSystemNoData;
            response.SuggestedQuestions = _text.ChatSystemSuggestions.ToList();
            return;
        }

        response.Reply = BuildSystemReply(hits);
        response.SuggestedQuestions = BuildSystemSuggestions(hits.First().Entry);
        response.RecommendedTours = [];
    }

    private bool TryAnswerFromSystemKnowledge(ChatResponseDTO response, string message)
    {
        var hits = _systemKnowledge.Retrieve(message, topK: 3);
        if (hits.Count == 0 || hits[0].Score < 0.08f)
        {
            return false;
        }

        response.Intent = TourIntents.AskSystem;
        response.Reply = BuildSystemReply(hits);
        response.SuggestedQuestions = BuildSystemSuggestions(hits.First().Entry);
        response.RecommendedTours = [];
        return true;
    }

    private async Task HandleCultureQuestionAsync(
        ChatResponseDTO response,
        string message,
        ParsedQueryDTO parsed,
        CancellationToken cancellationToken)
    {
        response.TourismInsights = await _searchService.SearchTourismAsync(
            message,
            parsed.City,
            5,
            cancellationToken);

        var ragHits = _culturalKnowledge.SearchRag(
            message,
            parsed.City,
            interests: null,
            forForeignVisitor: false,
            forElderly: false,
            forChildren: false,
            topK: 4)
            .Where(r => string.IsNullOrWhiteSpace(parsed.City) ||
                        r.Chunk.CityKeys.Any(k => VietnameseTextNormalizer.CityEquals(k, parsed.City)))
            .ToList();

        foreach (var rag in ragHits.Take(3))
        {
            var fact = _knowledgeLocalization.LocalizeRagFact(
                rag.Chunk.Id,
                rag.Chunk.Title,
                rag.Chunk.Content);

            response.TourismInsights.Add(new TourismInsightDTO
            {
                Id = 0,
                Name = rag.Chunk.Title,
                Type = rag.Chunk.ChunkType,
                Description = fact,
                City = rag.Chunk.CityKeys.FirstOrDefault() ?? rag.Chunk.Region,
                SourceName = rag.Chunk.Source.Name,
                SourceUrl = rag.Chunk.Source.Url,
                RelevanceScore = rag.Score,
                AuthorityLevel = rag.Chunk.Source.Authority,
                KnowledgeProvider = "RAG-Corpus"
            });
        }

        response.RecommendedTours = (await _searchService.SearchAsync(new NaturalLanguageSearchRequestDTO
        {
            Query = message,
            Top = 5,
            City = parsed.City,
            Country = parsed.Country,
            MaxPrice = parsed.MaxPrice,
            DurationDays = parsed.DurationDays
        }, cancellationToken)).Select(MapToSearchItem).ToList();

        response.Reply = BuildCultureReply(response.TourismInsights, parsed.City, ragHits);
        response.SuggestedQuestions =
        [
            _text.ChatCultureSuggest1,
            _text.ChatCultureSuggest2,
            _text.ChatCultureSuggest3
        ];
    }

    private async Task HandleTourSearchAsync(
        ChatResponseDTO response,
        string message,
        ParsedQueryDTO parsed,
        CancellationToken cancellationToken)
    {
        response.RecommendedTours = (await _searchService.SearchAsync(new NaturalLanguageSearchRequestDTO
        {
            Query = message,
            Top = 6,
            City = parsed.City,
            Country = parsed.Country,
            MinPrice = parsed.MinPrice,
            MaxPrice = parsed.MaxPrice,
            DurationDays = parsed.DurationDays,
            GroupSize = parsed.GroupSize,
            StartDate = parsed.StartDate,
            EndDate = parsed.EndDate
        }, cancellationToken)).Select(MapToSearchItem).ToList();

        response.Reply = response.RecommendedTours.Count > 0
            ? (!string.IsNullOrWhiteSpace(parsed.City)
                ? _text.ChatToursFoundForCity(response.RecommendedTours.Count, parsed.City)
                : _text.ChatToursFound(response.RecommendedTours.Count))
            : (!string.IsNullOrWhiteSpace(parsed.City)
                ? _text.ChatNoToursForCity(parsed.City)
                : _text.ChatNoTours);
        response.SuggestedQuestions = _text.ChatDefaultSuggestions.ToList();
    }

    private async Task HandleRecommendationAsync(
        ChatResponseDTO response,
        string message,
        ParsedQueryDTO parsed,
        CancellationToken cancellationToken)
    {
        response.RecommendedTours = (await _searchService.SearchAsync(new NaturalLanguageSearchRequestDTO
        {
            Query = message,
            Top = 6,
            City = parsed.City,
            Country = parsed.Country,
            MinPrice = parsed.MinPrice,
            MaxPrice = parsed.MaxPrice,
            DurationDays = parsed.DurationDays,
            GroupSize = parsed.GroupSize,
            StartDate = parsed.StartDate,
            EndDate = parsed.EndDate,
            CategoryId = parsed.CategoryId
        }, cancellationToken)).Select(MapToSearchItem).ToList();

        response.Reply = response.RecommendedTours.Count > 0
            ? _text.ChatRecommendFound(response.RecommendedTours.Count)
            : _text.ChatRecommendNone;

        response.SuggestedQuestions = _text.ChatDefaultSuggestions.ToList();
    }

    public async Task<List<TourRecommendationItemDTO>> ConsultAsync(
        TourConsultationRequestDTO request,
        CancellationToken cancellationToken = default)
    {
        var profile = ConsultProfileMapper.ToQuestionnaire(request);
        var response = await _personalizedService.RecommendFromProfileAsync(profile, cancellationToken);
        return response.RecommendedTours
            .Concat(response.NearbyScheduleTours)
            .Take(request.Top)
            .ToList();
    }

    private string BuildSystemReply(IReadOnlyList<SystemKnowledgeHit> hits)
    {
        var top = hits[0];
        var title = _text.IsVietnamese ? top.Entry.TitleVi : top.Entry.Title;
        var content = _text.IsVietnamese ? top.Entry.ContentVi : top.Entry.Content;

        if (hits.Count == 1)
        {
            return _text.ChatSystemReply(title, content);
        }

        var parts = new List<string> { _text.ChatSystemMultiHeader, _text.ChatSystemReply(title, content) };
        foreach (var extra in hits.Skip(1).Take(2))
        {
            var extraTitle = _text.IsVietnamese ? extra.Entry.TitleVi : extra.Entry.Title;
            var extraContent = _text.IsVietnamese ? extra.Entry.ContentVi : extra.Entry.Content;
            parts.Add(_text.ChatSystemReply(extraTitle, Trim(extraContent, 180)));
        }

        return string.Join("\n\n", parts);
    }

    private List<string> BuildSystemSuggestions(SystemKnowledgeEntry entry)
    {
        var related = _text.IsVietnamese ? entry.RelatedQuestionsVi : entry.RelatedQuestions;
        if (related.Count > 0)
        {
            return related.Take(3).ToList();
        }

        return _text.ChatSystemSuggestions.Take(3).ToList();
    }

    private string BuildCultureReply(
        IReadOnlyList<TourismInsightDTO> insights,
        string? city,
        IReadOnlyList<RagRetrievalResult> ragHits)
    {
        if (insights.Count == 0 && ragHits.Count == 0)
        {
            return string.IsNullOrWhiteSpace(city)
                ? _text.ChatCultureNoData
                : _text.ChatCultureNoDataForCity(city);
        }

        var parts = new List<string>();

        if (ragHits.Count > 0)
        {
            var rag = ragHits[0];
            var fact = _knowledgeLocalization.LocalizeRagFact(rag.Chunk.Id, rag.Chunk.Title, rag.Chunk.Content);
            parts.Add(_text.ChatRagInsight(Trim(fact, 280)));
        }

        if (insights.Count > 0)
        {
            var top = insights[0];
            var source = string.IsNullOrWhiteSpace(top.SourceName)
                ? (_text.IsVietnamese ? "nguồn tham khảo hệ thống" : "system reference")
                : top.SourceName;
            parts.Add(_text.ChatCultureReply(source, top.Name, top.Type, Trim(top.Description, 220)));
        }

        if (insights.Count > 1)
        {
            var second = insights[1];
            parts.Add(_text.ChatRagInsight(Trim($"{second.Name}: {second.Description}", 200)));
        }

        return string.Join("\n\n", parts);
    }

    private static string Trim(string? text, int max) =>
        string.IsNullOrWhiteSpace(text) ? "" : text.Length <= max ? text : text[..max] + "...";

    private static TourSearchResultItemDTO MapToSearchItem(TourSearchResultItemDTO item) => item;
}
