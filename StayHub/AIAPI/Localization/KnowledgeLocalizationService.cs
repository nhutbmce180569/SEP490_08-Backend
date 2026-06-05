using System.Text.Json;
using AIAPI.DTOs;
using AIAPI.Models.Knowledge;

namespace AIAPI.Localization;

public interface IKnowledgeLocalizationService
{
    bool IsVietnamese { get; }
    string LocalizeRagFact(string chunkId, string title, string content);
    string LocalizeEmbeddedFact(string englishFact, IReadOnlyList<string>? factsVi, int index);
    CulturalFactResult LocalizeFact(CulturalFactResult fact);
    TourismInsightDTO LocalizeInsight(TourismInsightDTO insight);
    string LocalizeInsightType(string? type);
    string LocalizeAuthority(string? authority);
    string LocalizeCityDisplay(string? city);
    string LocalizeTourismContent(string name, string? description, string? type);
}

public sealed class KnowledgeLocalizationService : IKnowledgeLocalizationService
{
    private readonly Dictionary<string, RagChunkViOverlay> _ragVi;
    private readonly Dictionary<string, string> _embeddedFactMap;

    public KnowledgeLocalizationService(IAiCultureAccessor culture)
    {
        IsVietnamese = culture.IsVietnamese;
        _ragVi = LoadRagViOverlay();
        _embeddedFactMap = LoadEmbeddedFactMap();
    }

    public bool IsVietnamese { get; }

    public string LocalizeRagFact(string chunkId, string title, string content)
    {
        if (!IsVietnamese)
        {
            return $"{title}: {content}";
        }

        if (_ragVi.TryGetValue(chunkId, out var vi) &&
            !string.IsNullOrWhiteSpace(vi.TitleVi) &&
            !string.IsNullOrWhiteSpace(vi.ContentVi))
        {
            return $"{vi.TitleVi}: {vi.ContentVi}";
        }

        return $"{title}: {content}";
    }

    public string LocalizeEmbeddedFact(string englishFact, IReadOnlyList<string>? factsVi, int index)
    {
        if (!IsVietnamese)
        {
            return englishFact;
        }

        if (factsVi != null && index >= 0 && index < factsVi.Count && !string.IsNullOrWhiteSpace(factsVi[index]))
        {
            return factsVi[index];
        }

        return _embeddedFactMap.GetValueOrDefault(englishFact, englishFact);
    }

    public CulturalFactResult LocalizeFact(CulturalFactResult fact)
    {
        if (!IsVietnamese)
        {
            return fact;
        }

        return new CulturalFactResult
        {
            Fact = fact.Fact,
            SourceName = fact.SourceName,
            SourceUrl = fact.SourceUrl,
            AuthorityLevel = LocalizeAuthority(fact.AuthorityLevel),
            Provider = fact.Provider,
            City = LocalizeCityDisplay(fact.City)
        };
    }

    public TourismInsightDTO LocalizeInsight(TourismInsightDTO insight)
    {
        if (!IsVietnamese)
        {
            return insight;
        }

        return new TourismInsightDTO
        {
            Id = insight.Id,
            Name = !string.IsNullOrWhiteSpace(insight.City)
                ? LocalizeCityDisplay(insight.City)
                : insight.Name is "Cultural insight" or "Knowledge"
                    ? "Gợi ý du lịch"
                    : insight.Name,
            Type = LocalizeInsightType(insight.Type),
            Description = insight.Description,
            City = LocalizeCityDisplay(insight.City),
            SourceName = insight.SourceName,
            SourceUrl = insight.SourceUrl,
            AuthorityLevel = LocalizeAuthority(insight.AuthorityLevel),
            KnowledgeProvider = insight.KnowledgeProvider,
            RelevanceScore = insight.RelevanceScore
        };
    }

    public string LocalizeInsightType(string? type) => (type ?? "").ToLowerInvariant() switch
    {
        "knowledge" => IsVietnamese ? "Kiến thức địa phương" : "Knowledge",
        "destination" => IsVietnamese ? "Điểm đến" : "Destination",
        "localfood" => IsVietnamese ? "Ẩm thực địa phương" : "LocalFood",
        "culture" => IsVietnamese ? "Văn hóa" : "Culture",
        _ => type ?? (IsVietnamese ? "Gợi ý du lịch" : "Insight")
    };

    public string LocalizeAuthority(string? authority) => (authority ?? "").ToLowerInvariant() switch
    {
        "international" => IsVietnamese ? "Quốc tế" : "International",
        "national" => IsVietnamese ? "Quốc gia" : "National",
        "curated" => IsVietnamese ? "Biên soạn" : "Curated",
        "curated-rag" => IsVietnamese ? "Tri thức RAG" : "Curated-RAG",
        "platform-curated" => IsVietnamese ? "Nền tảng" : "Platform-Curated",
        "international-cc0" => IsVietnamese ? "Wikidata (CC0)" : "International-CC0",
        _ => authority ?? ""
    };

    public string LocalizeCityDisplay(string? city)
    {
        if (string.IsNullOrWhiteSpace(city) || !IsVietnamese)
        {
            return city ?? "";
        }

        return city.ToLowerInvariant() switch
        {
            "can tho" or "cantho" => "Cần Thơ",
            "hoi an" or "hoian" => "Hội An",
            "ha long" or "halong" => "Hạ Long",
            "hue" => "Huế",
            "da lat" or "dalat" => "Đà Lạt",
            "da nang" or "danang" => "Đà Nẵng",
            "hanoi" or "ha noi" => "Hà Nội",
            "ho chi minh city" or "hochiminh" or "saigon" => "TP. Hồ Chí Minh",
            "phu quoc" or "phuquoc" => "Phú Quốc",
            "nha trang" or "nhatrang" => "Nha Trang",
            "ninh binh" or "ninhbinh" => "Ninh Bình",
            "sapa" => "Sa Pa",
            _ => city
        };
    }

    public string LocalizeTourismContent(string name, string? description, string? type)
    {
        if (!IsVietnamese)
        {
            return string.IsNullOrWhiteSpace(description) ? name : $"{name}: {description}";
        }

        var localizedName = LocalizeTourismName(name);
        if (string.IsNullOrWhiteSpace(description))
        {
            return localizedName;
        }

        // ContentAPI descriptions are often English — prefix with localized name for readability.
        return $"{localizedName}: {description}";
    }

    private static string LocalizeTourismName(string name) => name switch
    {
        "Cai Rang Floating Market" => "Chợ nổi Cái Ràng",
        "Mekong Delta Pancake (Banh Xeo)" => "Bánh xèo miền Tây",
        _ => name
    };

    private static Dictionary<string, RagChunkViOverlay> LoadRagViOverlay()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "vietnam-rag-corpus-vi.json");
        if (!File.Exists(path))
        {
            return new Dictionary<string, RagChunkViOverlay>(StringComparer.OrdinalIgnoreCase);
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, RagChunkViOverlay>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new Dictionary<string, RagChunkViOverlay>(StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> LoadEmbeddedFactMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "vietnam-cultural-knowledge.json");
        if (!File.Exists(path))
        {
            return map;
        }

        var bundle = JsonSerializer.Deserialize<CulturalKnowledgeBundleVi>(File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (bundle?.Entries == null)
        {
            return map;
        }

        foreach (var entry in bundle.Entries)
        {
            if (entry.Facts == null || entry.FactsVi == null)
            {
                continue;
            }

            for (var i = 0; i < Math.Min(entry.Facts.Count, entry.FactsVi.Count); i++)
            {
                if (!string.IsNullOrWhiteSpace(entry.Facts[i]) && !string.IsNullOrWhiteSpace(entry.FactsVi[i]))
                {
                    map.TryAdd(entry.Facts[i], entry.FactsVi[i]);
                }
            }
        }

        return map;
    }

    private sealed class RagChunkViOverlay
    {
        public string TitleVi { get; set; } = "";
        public string ContentVi { get; set; } = "";
    }

    private sealed class CulturalKnowledgeBundleVi
    {
        public List<CulturalKnowledgeEntryVi> Entries { get; set; } = new();
    }

    private sealed class CulturalKnowledgeEntryVi
    {
        public List<string> Facts { get; set; } = new();
        public List<string> FactsVi { get; set; } = new();
    }
}
