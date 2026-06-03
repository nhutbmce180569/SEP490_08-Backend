using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Localization;
using AIAPI.ML;
using AIAPI.Models;
using AIAPI.Services;

namespace AIAPI.Services.Implements;

public class TourAssistantService : ITourAssistantService
{
    private readonly IMlModelRegistry _modelRegistry;
    private readonly ICatalogStore _catalogStore;
    private readonly ITourSemanticSearchService _searchService;
    private readonly ITourRecommendationService _recommendationService;
    private readonly QueryEntityExtractor _entityExtractor;
    private readonly StayHubAiDbContext _dbContext;
    private readonly IAiLocalizedCopy _text;

    public TourAssistantService(
        IMlModelRegistry modelRegistry,
        ICatalogStore catalogStore,
        ITourSemanticSearchService searchService,
        ITourRecommendationService recommendationService,
        QueryEntityExtractor entityExtractor,
        StayHubAiDbContext dbContext,
        IAiLocalizedCopy text)
    {
        _modelRegistry = modelRegistry;
        _catalogStore = catalogStore;
        _searchService = searchService;
        _recommendationService = recommendationService;
        _entityExtractor = entityExtractor;
        _dbContext = dbContext;
        _text = text;
    }

    public async Task<ChatResponseDTO> ChatAsync(string message, string? sessionId, int? customerId, CancellationToken cancellationToken = default)
    {
        if (!_catalogStore.IsReady || !_modelRegistry.Status.IsReady)
        {
            throw new InvalidOperationException("AI assistant is warming up. Please retry in a few seconds.");
        }

        var session = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString("N") : sessionId;
        var (intent, confidence) = _modelRegistry.PredictIntent(message);
        var parsed = _entityExtractor.Extract(message, _catalogStore);

        var response = new ChatResponseDTO
        {
            SessionId = session,
            Intent = intent,
            IntentConfidence = confidence,
            ParsedQuery = parsed,
            UsedPersonalization = customerId.HasValue
        };

        switch (intent)
        {
            case TourIntents.Greeting:
                response.Reply = _text.ChatGreeting;
                response.SuggestedQuestions = _text.ChatDefaultSuggestions.ToList();
                break;

            case TourIntents.AskCulture:
            case TourIntents.AskDestination:
                response.TourismInsights = await _searchService.SearchTourismAsync(
                    message,
                    parsed.City,
                    5,
                    cancellationToken);

                response.RecommendedTours = (await _searchService.SearchAsync(new NaturalLanguageSearchRequestDTO
                {
                    Query = message,
                    Top = 5,
                    City = parsed.City,
                    Country = parsed.Country,
                    MaxPrice = parsed.MaxPrice,
                    DurationDays = parsed.DurationDays
                }, cancellationToken)).Select(MapToSearchItem).ToList();

                response.Reply = BuildCultureReply(response.TourismInsights, parsed.City);
                response.SuggestedQuestions =
                [
                    _text.ChatCultureSuggest1,
                    _text.ChatCultureSuggest2,
                    _text.ChatCultureSuggest3
                ];
                break;

            case TourIntents.AskBudget:
            case TourIntents.SearchTour:
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
                    ? _text.ChatToursFound(response.RecommendedTours.Count)
                    : _text.ChatNoTours;
                response.SuggestedQuestions = _text.ChatDefaultSuggestions.ToList();
                break;

            case TourIntents.RecommendTour:
            default:
                var recommended = await _recommendationService.RecommendAsync(customerId, 6, parsed, cancellationToken);
                response.RecommendedTours = recommended.Select(MapRecommendationToSearch).ToList();
                response.Reply = customerId.HasValue
                    ? _text.ChatPersonalizedLoggedIn
                    : _text.ChatPersonalizedAnonymous;
                response.SuggestedQuestions = _text.ChatDefaultSuggestions.ToList();
                break;
        }

        foreach (var tour in response.RecommendedTours.Take(3))
        {
            await LogInteractionAsync(customerId, new LogInteractionRequestDTO
            {
                TourId = tour.TourId,
                InteractionType = "chat_recommend",
                SessionId = session
            }, cancellationToken);
        }

        return response;
    }

    public async Task<List<TourRecommendationItemDTO>> ConsultAsync(
        TourConsultationRequestDTO request,
        int? customerId,
        CancellationToken cancellationToken = default)
    {
        var parsed = _entityExtractor.Merge(new ParsedQueryDTO(), request);
        if (!string.IsNullOrWhiteSpace(request.TravelStyle))
        {
            var styleMatches = _modelRegistry.SearchTours(request.TravelStyle, 20);
            var styleTourIds = styleMatches.Select(m => m.TourId).ToHashSet();
            var filtered = _catalogStore.Tours.Where(t => styleTourIds.Contains(t.Id)).Select(t => t.Id).ToHashSet();

            var recommendations = await _recommendationService.RecommendAsync(customerId, request.Top * 2, parsed, cancellationToken);
            return recommendations.Where(r => filtered.Contains(r.TourId)).Take(request.Top).ToList();
        }

        return await _recommendationService.RecommendAsync(customerId, request.Top, parsed, cancellationToken);
    }

    public async Task LogInteractionAsync(int? customerId, LogInteractionRequestDTO request, CancellationToken cancellationToken = default)
    {
        var catalogTourId = CatalogTourIds.ResolveBaseTourId(request.TourId);
        if (!_catalogStore.Tours.Any(t => t.Id == catalogTourId || t.Id == request.TourId))
        {
            throw new InvalidOperationException("TourId does not exist in active catalog.");
        }

        var weight = request.InteractionType switch
        {
            "booking" => 5f,
            "wishlist" => 4f,
            "chat_recommend" => 2f,
            "click" => 1.5f,
            _ => 1f
        };

        _dbContext.UserTourInteractions.Add(new UserTourInteraction
        {
            CustomerId = customerId,
            TourId = catalogTourId,
            InteractionType = request.InteractionType,
            Weight = weight,
            SessionId = request.SessionId,
            CreatedAt = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private string BuildCultureReply(IReadOnlyList<TourismInsightDTO> insights, string? city)
    {
        if (insights.Count == 0)
        {
            return string.IsNullOrWhiteSpace(city)
                ? _text.ChatCultureNoData
                : _text.ChatCultureNoDataForCity(city);
        }

        var top = insights[0];
        var source = string.IsNullOrWhiteSpace(top.SourceName)
            ? (_text.IsVietnamese ? "nguồn tham khảo hệ thống" : "system reference")
            : top.SourceName;
        return _text.ChatCultureReply(source, top.Name, top.Type, Trim(top.Description, 220));
    }

    private static string Trim(string? text, int max) =>
        string.IsNullOrWhiteSpace(text) ? "" : text.Length <= max ? text : text[..max] + "...";

    private static TourSearchResultItemDTO MapToSearchItem(TourSearchResultItemDTO item) => item;

    private static TourSearchResultItemDTO MapRecommendationToSearch(TourRecommendationItemDTO item) => new()
    {
        TourId = CatalogTourIds.ResolveBaseTourId(item.TourId),
        Name = item.Name,
        City = item.City,
        Country = item.Country,
        ImageUrl = item.ImageUrl,
        AverageStar = item.AverageStar,
        MinPrice = item.MinPrice,
        DurationDays = item.DurationDays,
        Score = item.Score,
        SemanticScore = item.Score,
        Reason = item.Reason
    };
}
