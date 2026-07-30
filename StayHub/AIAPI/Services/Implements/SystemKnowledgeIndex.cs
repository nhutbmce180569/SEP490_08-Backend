using System.Text.Json;
using AIAPI.Helpers;
using AIAPI.ML;
using AIAPI.Models.Knowledge;
using Microsoft.ML;

namespace AIAPI.Services.Implements;

public class SystemKnowledgeIndex : ISystemKnowledgeIndex
{
    private readonly ILocalEmbeddingService _embeddingService;
    private readonly object _lock = new();
    private List<(SystemKnowledgeEntry Entry, float[] Vector)> _indexed = new();
    private List<(SystemKnowledgeEntry Entry, string NormalizedPrompt)> _promptIndex = new();
    private SystemKnowledgeBundle _bundle = new();

    public SystemKnowledgeIndex(ILocalEmbeddingService embeddingService)
    {
        _embeddingService = embeddingService;
    }

    public bool IsReady { get; private set; }
    public int EntryCount { get; private set; }

    public void Initialize()
    {
        lock (_lock)
        {
            IsReady = false;
            _indexed = new List<(SystemKnowledgeEntry Entry, float[] Vector)>();
            _promptIndex = new List<(SystemKnowledgeEntry Entry, string NormalizedPrompt)>();
            _bundle = LoadBundle();

            if (_bundle.Entries.Count == 0)
            {
                EntryCount = 0;
                return;
            }

            _indexed = _bundle.Entries
                .Select(e => (e, _embeddingService.EmbedText(e.BuildSearchDocument())))
                .ToList();

            _promptIndex = _bundle.Entries
                .SelectMany(e => e.FaqPrompts
                    .Concat(e.FaqPromptsVi)
                    .Select(prompt => (Entry: e, NormalizedPrompt: VietnameseTextNormalizer.Normalize(prompt))))
                .Where(x => !string.IsNullOrWhiteSpace(x.NormalizedPrompt))
                .ToList();

            EntryCount = _indexed.Count;
            IsReady = true;
        }
    }

    public IReadOnlyList<SystemKnowledgeHit> Retrieve(string query, int topK = 4)
    {
        lock (_lock)
        {
            if (!IsReady || _indexed.Count == 0 || string.IsNullOrWhiteSpace(query))
            {
                return Array.Empty<SystemKnowledgeHit>();
            }

            var normalizedQuery = VietnameseTextNormalizer.Normalize(query);
            var promptHit = MatchFaqPrompt(normalizedQuery);
            if (promptHit != null)
            {
                return [promptHit];
            }

            var queryVector = _embeddingService.EmbedText(query);

            return _indexed
                .Select(item =>
                {
                    var semantic = _embeddingService.CosineSimilarity(queryVector, item.Vector);
                    var keywordBoost = ComputeKeywordBoost(normalizedQuery, item.Entry);
                    var titleBoost = ComputeTitleBoost(normalizedQuery, item.Entry);
                    return new SystemKnowledgeHit
                    {
                        Entry = item.Entry,
                        Score = semantic * 0.55f + keywordBoost * 0.3f + titleBoost * 0.15f
                    };
                })
                .Where(h => h.Score > 0.05f)
                .OrderByDescending(h => h.Score)
                .Take(topK)
                .ToList();
        }
    }

    private SystemKnowledgeHit? MatchFaqPrompt(string normalizedQuery)
    {
        SystemKnowledgeHit? best = null;

        foreach (var (entry, prompt) in _promptIndex)
        {
            if (prompt == normalizedQuery)
            {
                return new SystemKnowledgeHit { Entry = entry, Score = 1f };
            }

            if (normalizedQuery.Contains(prompt, StringComparison.Ordinal) ||
                prompt.Contains(normalizedQuery, StringComparison.Ordinal))
            {
                best = new SystemKnowledgeHit { Entry = entry, Score = 0.95f };
                continue;
            }

            var overlap = ComputeOverlapScore(normalizedQuery, prompt);
            if (overlap >= 0.72f && (best == null || overlap > best.Score))
            {
                best = new SystemKnowledgeHit { Entry = entry, Score = overlap };
            }
        }

        return best;
    }

    private static float ComputeOverlapScore(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
        {
            return 0f;
        }

        var shorter = a.Length <= b.Length ? a : b;
        var longer = a.Length > b.Length ? a : b;
        var matched = 0;

        for (var i = 0; i < shorter.Length - 2; i++)
        {
            var tri = shorter.Substring(i, Math.Min(3, shorter.Length - i));
            if (tri.Length >= 3 && longer.Contains(tri, StringComparison.Ordinal))
            {
                matched++;
            }
        }

        var expected = Math.Max(shorter.Length / 3, 1);
        return Math.Min(matched / (float)expected, 1f);
    }

    private static float ComputeKeywordBoost(string normalizedQuery, SystemKnowledgeEntry entry)
    {
        if (entry.Keywords.Count == 0)
        {
            return 0f;
        }

        var hits = entry.Keywords.Count(k =>
        {
            var key = VietnameseTextNormalizer.Normalize(k);
            return normalizedQuery.Contains(key, StringComparison.Ordinal) ||
                   key.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                       .Any(part => part.Length >= 3 && normalizedQuery.Contains(part, StringComparison.Ordinal));
        });

        return Math.Min(hits / (float)Math.Max(entry.Keywords.Count, 1), 1f);
    }

    private static float ComputeTitleBoost(string normalizedQuery, SystemKnowledgeEntry entry)
    {
        var titles = new[] { entry.Title, entry.TitleVi }
            .Select(VietnameseTextNormalizer.Normalize)
            .Where(t => !string.IsNullOrWhiteSpace(t));

        return titles.Any(t => normalizedQuery.Contains(t, StringComparison.Ordinal) ||
                               t.Contains(normalizedQuery, StringComparison.Ordinal))
            ? 1f
            : 0f;
    }

    private static SystemKnowledgeBundle LoadBundle()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", "stayhub-system-knowledge.json");
        if (!File.Exists(path))
        {
            return new SystemKnowledgeBundle();
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SystemKnowledgeBundle>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new SystemKnowledgeBundle();
    }
}
