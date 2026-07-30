namespace AIAPI.Recommender;

public static class InterestMatchHelper
{
    public static readonly IReadOnlyDictionary<string, string[]> Keywords = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["beach"] = ["beach", "sea", "island", "resort", "biển", "đảo"],
        ["culture"] = ["culture", "heritage", "ancient", "temple", "unesco", "văn hóa", "di tích", "phố cổ"],
        ["nature"] = ["nature", "mountain", "trek", "cloud", "forest", "thiên nhiên", "núi"],
        ["food"] = ["food", "cuisine", "street food", "ẩm thực", "đặc sản", "seafood"],
        ["adventure"] = ["adventure", "trek", "climb", "dive", "mạo hiểm", "kayak"],
        ["relax"] = ["relax", "spa", "resort", "chill", "nghỉ dưỡng", "honeymoon", "cruise"],
        ["photography"] = ["photo", "sunset", "cloud hunting", "chụp ảnh"],
        ["city"] = ["city", "night market", "thành phố", "chợ đêm"],
        ["river"] = ["river", "mekong", "floating market", "sông", "chợ nổi", "delta"]
    };

    /// <summary>
    /// Expands interest tags into descriptive keywords for semantic search (TF-IDF cosine).
    /// Raw tags like "beach culture" score poorly against full tour documents.
    /// </summary>
    public static string BuildSearchQuery(IEnumerable<string> interests)
    {
        var parts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var interest in interests)
        {
            if (Keywords.TryGetValue(interest, out var keys))
            {
                foreach (var key in keys)
                {
                    parts.Add(key);
                }
            }
            else if (!string.IsNullOrWhiteSpace(interest))
            {
                parts.Add(interest);
            }
        }

        return parts.Count > 0 ? string.Join(" ", parts) : "travel tour vietnam";
    }

    public static float ScoreKeywordMatch(string doc, IEnumerable<string> interests)
    {
        var list = interests.ToList();
        if (list.Count == 0)
        {
            return 1f;
        }

        var hits = CountMatchedInterests(doc, list);
        return Math.Clamp(hits / Math.Min(3f, list.Count), 0f, 1f);
    }

    public static int CountMatchedInterests(string doc, IEnumerable<string> interests)
    {
        var normalized = doc.ToLowerInvariant();
        var count = 0;

        foreach (var interest in interests)
        {
            if (!Keywords.TryGetValue(interest, out var keys))
            {
                continue;
            }

            if (keys.Any(k => normalized.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                count++;
            }
        }

        return count;
    }

    public static IReadOnlyList<string> GetMatchedInterestKeys(string doc, IEnumerable<string> interests) =>
        interests
            .Where(i => Keywords.TryGetValue(i, out var keys) &&
                        keys.Any(k => doc.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .ToList();
}
