using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIAPI.DTOs;
using AIAPI.Helpers;
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

    public CulturalKnowledgeService(
        IHttpClientFactory httpClientFactory,
        ITourSemanticSearchService contentSearch,
        IRagKnowledgeIndex ragIndex,
        IOptions<RecommenderSettings> settings)
    {
        _wikidataClient = httpClientFactory.CreateClient("Wikidata");
        _contentSearch = contentSearch;
        _ragIndex = ragIndex;
        _settings = settings.Value;
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
                query, city, interestList, forForeignVisitor, forElderly, forChildren, topK: 5);

            results.AddRange(ragHits.Select(r => new CulturalFactResult
            {
                Fact = $"{r.Chunk.Title}: {r.Chunk.Content}",
                SourceName = r.Chunk.Source.Name,
                SourceUrl = r.Chunk.Source.Url,
                AuthorityLevel = r.Chunk.Source.Authority,
                Provider = ScoringModelSpec.KnowledgeSources.RagCorpus,
                City = city ?? r.Chunk.Region
            }));
        }

        var entry = ResolveEmbeddedEntry(city);
        if (entry != null)
        {
            foreach (var fact in entry.Facts.Take(2))
            {
                results.Add(MapEmbedded(fact, entry, city, "general"));
            }

            if (forForeignVisitor)
            {
                results.AddRange(entry.ForeignVisitorNotes.Take(1).Select(n => MapEmbedded(n, entry, city, "foreign_visitor")));
            }

            if (forElderly)
            {
                results.AddRange(entry.ElderlyNotes.Take(1).Select(n => MapEmbedded(n, entry, city, "elderly")));
            }

            if (forChildren)
            {
                results.AddRange(entry.ChildNotes.Take(1).Select(n => MapEmbedded(n, entry, city, "children")));
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
            results.AddRange(contentFacts.Select(f => new CulturalFactResult
            {
                Fact = string.IsNullOrWhiteSpace(f.Description) ? f.Name : $"{f.Name}: {f.Description}",
                SourceName = f.SourceName ?? "StayHub ContentAPI",
                SourceUrl = f.SourceUrl ?? "",
                AuthorityLevel = "Platform-Curated",
                Provider = ScoringModelSpec.KnowledgeSources.ContentApi,
                City = f.City ?? city
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

        return results
            .GroupBy(r => r.Fact, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Take(12)
            .ToList();
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

    private static CulturalFactResult MapEmbedded(string fact, CulturalKnowledgeEntry entry, string? city, string category)
    {
        var source = entry.Sources.FirstOrDefault() ?? new CulturalSourceRef
        {
            Name = "StayHub VN Cultural Corpus",
            Url = "https://stayhub.vn/ai/knowledge",
            Authority = "Curated"
        };

        return new CulturalFactResult
        {
            Fact = fact,
            SourceName = source.Name,
            SourceUrl = source.Url,
            AuthorityLevel = source.Authority,
            Provider = ScoringModelSpec.KnowledgeSources.EmbeddedCorpus,
            City = city ?? entry.DisplayName
        };
    }

    private async Task<CulturalFactResult?> FetchWikidataSummaryAsync(string city, CancellationToken cancellationToken)
    {
        try
        {
            var url =
                $"https://www.wikidata.org/w/api.php?action=wbsearchentities&search={Uri.EscapeDataString(city + " Vietnam")}&language=en&format=json&limit=1";
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
                City = city
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
