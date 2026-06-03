using AIAPI.Settings;
using Microsoft.Extensions.Options;

namespace AIAPI.Recommender;

public interface IDimensionWeightProvider
{
    float InterestSemantic { get; }
    float Location { get; }
    float Budget { get; }
    float Schedule { get; }
    float Weather { get; }
    float Accessibility { get; }
    float CulturalFit { get; }

    IReadOnlyDictionary<string, float> AsDictionary();
    void Apply(IReadOnlyDictionary<string, float> weights);
}

public sealed class DimensionWeightProvider : IDimensionWeightProvider
{
    private readonly object _lock = new();
    private float _interestSemantic;
    private float _location;
    private float _budget;
    private float _schedule;
    private float _weather;
    private float _accessibility;
    private float _culturalFit;

    public DimensionWeightProvider(IOptions<RecommenderSettings> settings)
    {
        var w = settings.Value.DimensionWeights;
        _interestSemantic = w.InterestSemantic;
        _location = w.Location;
        _budget = w.Budget;
        _schedule = w.Schedule;
        _weather = w.Weather;
        _accessibility = w.Accessibility;
        _culturalFit = w.CulturalFit;
    }

    public float InterestSemantic => _interestSemantic;
    public float Location => _location;
    public float Budget => _budget;
    public float Schedule => _schedule;
    public float Weather => _weather;
    public float Accessibility => _accessibility;
    public float CulturalFit => _culturalFit;

    public IReadOnlyDictionary<string, float> AsDictionary()
    {
        lock (_lock)
        {
            return new Dictionary<string, float>
            {
                ["interest_semantic"] = _interestSemantic,
                ["location"] = _location,
                ["budget"] = _budget,
                ["schedule"] = _schedule,
                ["weather"] = _weather,
                ["accessibility"] = _accessibility,
                ["cultural_fit"] = _culturalFit
            };
        }
    }

    public void Apply(IReadOnlyDictionary<string, float> weights)
    {
        lock (_lock)
        {
            if (weights.TryGetValue("interest_semantic", out var v1))
            {
                _interestSemantic = v1;
            }

            if (weights.TryGetValue("location", out var v2))
            {
                _location = v2;
            }

            if (weights.TryGetValue("budget", out var v3))
            {
                _budget = v3;
            }

            if (weights.TryGetValue("schedule", out var v4))
            {
                _schedule = v4;
            }

            if (weights.TryGetValue("weather", out var v5))
            {
                _weather = v5;
            }

            if (weights.TryGetValue("accessibility", out var v6))
            {
                _accessibility = v6;
            }

            if (weights.TryGetValue("cultural_fit", out var v7))
            {
                _culturalFit = v7;
            }

            NormalizeLocked();
        }
    }

    private void NormalizeLocked()
    {
        var sum = _interestSemantic + _location + _budget + _schedule + _weather + _accessibility + _culturalFit;
        if (sum <= 0)
        {
            return;
        }

        _interestSemantic /= sum;
        _location /= sum;
        _budget /= sum;
        _schedule /= sum;
        _weather /= sum;
        _accessibility /= sum;
        _culturalFit /= sum;
    }
}
