using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Localization;
using AIAPI.Models.Knowledge;
using AIAPI.Recommender;
using AIAPI.Settings;
using Microsoft.Extensions.Options;

namespace AIAPI.Services.Implements;

public class CulturalKnowledgeService : ICulturalKnowledgeService
{
    private readonly HttpClient _wikidataClient;
    private readonly ITourSemanticSearchService _contentSearch;
    private readonly IRagKnowledgeIndex _ragIndex;
    private readonly RecommenderSettings _settings;
    private readonly CulturalKnowledgeBundle _embedded;
    private readonly IKnowledgeLocalizationService _localizer;

    public CulturalKnowledgeService(
        IHttpClientFactory httpClientFactory,
        ITourSemanticSearchService contentSearch,
        IRagKnowledgeIndex ragIndex,
        IOptions<RecommenderSettings> settings,
        IKnowledgeLocalizationService localizer)
    {
        _wikidataClient = httpClientFactory.CreateClient("Wikidata");
        _contentSearch = contentSearch;
        _ragIndex = ragIndex;
        _settings = settings.Value;
        _localizer = localizer;
        _embedded = LoadEmbeddedCorpus();
    }

    public IReadOnlyList<CulturalSourceRef> GetRegisteredSources()
    {
        var sources = _embedded.Entries
            .SelectMany(e => e.Sources)
            .Concat(_ragIndex.Corpus.Chunks.Select(c => c.Source))
            .DistinctBy(s => s.Url)
            .ToList();

        sources.Add(new CulturalSourceRef
        {
            Name = "StayHub VN RAG Corpus",
            Url = "https://stayhub.vn/ai/knowledge/rag",
            Authority = "Curated-RAG"
        });

        sources.Add(new CulturalSourceRef
        {
            Name = "Wikidata",
            Url = "https://www.wikidata.org/",
            Authority = "International-CC0"
        });

        sources.Add(new CulturalSourceRef
        {
            Name = "StayHub ContentAPI",
            Url = "https://stayhub.vn/content/tourism",
            Authority = "Platform-Curated"
        });

        return sources;
    }

    public RagCorpusStatsDTO GetCorpusStats(int contentApiTourismCount)
    {
        var ragStats = _ragIndex.Corpus.Statistics;
        return new RagCorpusStatsDTO
        {
            CorpusVersion = _ragIndex.Corpus.Version,
            CorpusName = _ragIndex.Corpus.PaperMetadata?.CorpusName ?? "StayHub-VN-RAG-Corpus",
            TotalChunks = ragStats?.TotalChunks ?? _ragIndex.Corpus.Chunks.Count,
            EmbeddedCorpusEntries = _embedded.Entries.Count,
            ContentApiTourismItems = contentApiTourismCount,
            UnescoChunks = ragStats?.UnescoChunks ?? 0,
            ChunkTypes = ragStats?.ChunkTypes ?? _ragIndex.Corpus.Chunks.Select(c => c.ChunkType).Distinct().ToList(),
            InterestCoverage = ragStats?.InterestCoverage ?? new List<string>(),
            PersonaTags = ragStats?.PersonaCoverage ?? new List<string>(),
            KnowledgeProviders =
            [
                ScoringModelSpec.KnowledgeSources.EmbeddedCorpus,
                ScoringModelSpec.KnowledgeSources.RagCorpus,
                ScoringModelSpec.KnowledgeSources.ContentApi,
                ScoringModelSpec.KnowledgeSources.Wikidata,
                ScoringModelSpec.KnowledgeSources.OpenMeteo
            ],
            PaperCitationHint =
                "Multi-source RAG: 75 curated chunks (UNESCO-cited) + 12 regional entries + ContentAPI + Wikidata CC0 + Open-Meteo."
        };
    }

    public IReadOnlyList<RagRetrievalResult> SearchRag(
        string query,
        string? city,
        IEnumerable<string>? interests,
        bool forForeignVisitor,
        bool forElderly,
        bool forChildren,
        int topK = 8) =>
        _ragIndex.Retrieve(query, city, interests, forForeignVisitor, forElderly, forChildren, topK);

    public async Task<IReadOnlyList<CulturalFactResult>> GetFactsAsync(
        string? city,
        bool forForeignVisitor,
        bool forElderly,
        bool forChildren,
        IEnumerable<string>? interests = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<CulturalFactResult>();
        var interestList = interests?.ToList() ?? new List<string>();
        var query = string.Join(" ", interestList);

        if (_ragIndex.IsReady)
        {
            var ragHits = _ragIndex.Retrieve(
                    query, city, interestList, forForeignVisitor, forElderly, forChildren, topK: 8)
                .Where(r => ChunkMatchesCity(r.Chunk, city))
                .Take(5);

            results.AddRange(ragHits.Select(r => new CulturalFactResult
            {
                Fact = _localizer.LocalizeRagFact(r.Chunk.Id, r.Chunk.Title, r.Chunk.Content),
                SourceName = r.Chunk.Source.Name,
                SourceUrl = r.Chunk.Source.Url,
                AuthorityLevel = r.Chunk.Source.Authority,
                Provider = ScoringModelSpec.KnowledgeSources.RagCorpus,
                City = _localizer.LocalizeCityDisplay(ResolveChunkDisplayCity(r.Chunk, city))
            }));
        }

        var entry = ResolveEmbeddedEntry(city);
        if (entry != null)
        {
            foreach (var (fact, index) in entry.Facts.Take(2).Select((f, i) => (f, i)))
            {
                results.Add(MapEmbedded(fact, entry, city, index, entry.FactsVi));
            }

            if (forForeignVisitor)
            {
                results.AddRange(entry.ForeignVisitorNotes.Select((n, i) =>
                    MapEmbedded(n, entry, city, i, entry.ForeignVisitorNotesVi)));
            }

            if (forElderly)
            {
                results.AddRange(entry.ElderlyNotes.Take(1).Select((n, i) =>
                    MapEmbedded(n, entry, city, i, entry.ElderlyNotesVi)));
            }

            if (forChildren)
            {
                results.AddRange(entry.ChildNotes.Take(1).Select((n, i) =>
                    MapEmbedded(n, entry, city, i, entry.ChildNotesVi)));
            }
        }

        var contentQuery = string.Join(" ", interestList);
        if (!string.IsNullOrWhiteSpace(city))
        {
            contentQuery = $"{city} {contentQuery} culture tourism";
        }

        if (!string.IsNullOrWhiteSpace(contentQuery))
        {
            var contentFacts = await _contentSearch.SearchTourismAsync(contentQuery, city, 4, cancellationToken);
            results.AddRange(contentFacts
                .Where(f => string.IsNullOrWhiteSpace(city) ||
                            VietnameseTextNormalizer.CityEquals(f.City, city))
                .Select(f => new CulturalFactResult
                {
                    Fact = _localizer.LocalizeTourismContent(f.Name, f.Description, f.Type),
                    SourceName = f.SourceName ?? "StayHub ContentAPI",
                    SourceUrl = f.SourceUrl ?? "",
                    AuthorityLevel = "Platform-Curated",
                    Provider = ScoringModelSpec.KnowledgeSources.ContentApi,
                    City = _localizer.LocalizeCityDisplay(f.City ?? city)
                }));
        }

        if (_settings.EnableWikidataEnrichment && !string.IsNullOrWhiteSpace(city))
        {
            var wiki = await FetchWikidataSummaryAsync(city, cancellationToken);
            if (wiki != null)
            {
                results.Add(wiki);
            }
        }

        var finalResults = results
            .Where(r => string.IsNullOrWhiteSpace(city) ||
                        string.IsNullOrWhiteSpace(r.City) ||
                        VietnameseTextNormalizer.CityEquals(r.City, city))
            .GroupBy(r => r.Fact, StringComparer.OrdinalIgnoreCase)
            .Select(g => _localizer.LocalizeFact(g.First()))
            .Take(12)
            .ToList();

        if (finalResults.Count == 0 && !string.IsNullOrWhiteSpace(city))
        {
            var fallbackCity = _localizer.LocalizeCityDisplay(city);
            
            var viFallbacks = new[]
            {
                $"Trải nghiệm nhịp sống địa phương và nét văn hóa đặc trưng tại {fallbackCity}. Hãy chuẩn bị tinh thần cho những khám phá đầy bất ngờ nhé!",
                $"Khám phá vẻ đẹp tiềm ẩn của {fallbackCity}. Một điểm đến mang đậm dấu ấn bản địa dành cho những ai thích sự yên bình.",
                $"{fallbackCity} không quá ồn ào nhưng lại sở hữu những nét độc đáo riêng về ẩm thực và cảnh quan đang chờ bạn.",
                $"Thả mình vào không gian mộc mạc của {fallbackCity}, nơi bạn có thể gắn kết hơn với thiên nhiên và con người nơi đây.",
                $"Một hành trình về với {fallbackCity} sẽ mang lại những góc nhìn mới mẻ và những kỷ niệm khó quên."
            };
            
            var enFallbacks = new[]
            {
                $"Experience the local lifestyle and unique culture in {fallbackCity}. Get ready for exciting discoveries!",
                $"Discover the hidden gems of {fallbackCity}. A destination with a strong local vibe for those seeking peace.",
                $"{fallbackCity} is not too crowded but has its own unique culinary and scenic charms waiting for you.",
                $"Immerse yourself in the rustic atmosphere of {fallbackCity}, where you can connect with nature and locals.",
                $"A journey to {fallbackCity} will bring fresh perspectives and unforgettable memories."
            };

            var hash = Math.Abs(fallbackCity.GetHashCode());
            var index = hash % viFallbacks.Length;

            finalResults.Add(new CulturalFactResult
            {
                Fact = _localizer.IsVietnamese ? viFallbacks[index] : enFallbacks[index],
                SourceName = "StayHub AI Guide",
                SourceUrl = "",
                AuthorityLevel = "AI-Generated",
                Provider = ScoringModelSpec.KnowledgeSources.EmbeddedCorpus,
                City = fallbackCity
            });
        }

        return finalResults;
    }

    public IReadOnlyList<string> GetForeignVisitorNotesForCity(string? city)
    {
        var entry = ResolveEmbeddedEntry(city);
        if (entry == null || entry.ForeignVisitorNotes.Count == 0)
        {
            return Array.Empty<string>();
        }

        return entry.ForeignVisitorNotes
            .Select((note, index) => _localizer.LocalizeEmbeddedFact(
                note, entry.ForeignVisitorNotesVi, index))
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ChunkMatchesCity(RagCorpusChunk chunk, string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return true;
        }

        return chunk.CityKeys.Any(k => VietnameseTextNormalizer.CityEquals(city, k));
    }

    private static string? ResolveChunkDisplayCity(RagCorpusChunk chunk, string? requestedCity)
    {
        if (!string.IsNullOrWhiteSpace(requestedCity) &&
            chunk.CityKeys.Any(k => VietnameseTextNormalizer.CityEquals(requestedCity, k)))
        {
            return requestedCity;
        }

        return chunk.CityKeys.FirstOrDefault() ?? chunk.Region;
    }

    private CulturalKnowledgeEntry? ResolveEmbeddedEntry(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        var key = VietnameseTextNormalizer.Normalize(city);
        return _embedded.Entries.FirstOrDefault(e =>
            e.CityKeys.Any(k => key.Contains(VietnameseTextNormalizer.Normalize(k), StringComparison.Ordinal) ||
                                VietnameseTextNormalizer.Normalize(k).Contains(key, StringComparison.Ordinal)));
    }

    private CulturalFactResult MapEmbedded(
        string fact,
        CulturalKnowledgeEntry entry,
        string? city,
        int index,
        IReadOnlyList<string>? factsVi)
    {
        var source = entry.Sources.FirstOrDefault() ?? new CulturalSourceRef
        {
            Name = "StayHub VN Cultural Corpus",
            Url = "https://stayhub.vn/ai/knowledge",
            Authority = "Curated"
        };

        var displayCity = _localizer.IsVietnamese
            ? entry.DisplayNameVi ?? entry.DisplayName
            : entry.DisplayName;

        return new CulturalFactResult
        {
            Fact = _localizer.LocalizeEmbeddedFact(fact, factsVi, index),
            SourceName = source.Name,
            SourceUrl = source.Url,
            AuthorityLevel = source.Authority,
            Provider = ScoringModelSpec.KnowledgeSources.EmbeddedCorpus,
            City = _localizer.LocalizeCityDisplay(city ?? displayCity)
        };
    }

    private async Task<CulturalFactResult?> FetchWikidataSummaryAsync(string city, CancellationToken cancellationToken)
    {
        try
        {
            var lang = _localizer.IsVietnamese ? "vi" : "en";
            var url =
                $"https://www.wikidata.org/w/api.php?action=wbsearchentities&search={Uri.EscapeDataString(city + " Vietnam")}&language={lang}&format=json&limit=1";
            var response = await _wikidataClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<WikidataSearchResponse>(cancellationToken);
            var entity = payload?.Search?.FirstOrDefault();
            if (entity == null || string.IsNullOrWhiteSpace(entity.Description))
            {
                return null;
            }

            return new CulturalFactResult
            {
                Fact = entity.Description,
                SourceName = "Wikidata",
                SourceUrl = $"https://www.wikidata.org/wiki/{entity.Id}",
                AuthorityLevel = "International-CC0",
                Provider = ScoringModelSpec.KnowledgeSources.Wikidata,
                City = _localizer.LocalizeCityDisplay(city)
            };
        }
        catch
        {
            return null;
        }
    }

    private static CulturalKnowledgeBundle LoadEmbeddedCorpus()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "vietnam-cultural-knowledge.json");
        if (!File.Exists(path))
        {
            return new CulturalKnowledgeBundle();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<CulturalKnowledgeBundle>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new CulturalKnowledgeBundle();
    }

    private sealed class WikidataSearchResponse
    {
        [JsonPropertyName("search")]
        public List<WikidataEntity>? Search { get; set; }
    }

    private sealed class WikidataEntity
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }
}
