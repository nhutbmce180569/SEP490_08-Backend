using System.Globalization;
using System.Text.RegularExpressions;
using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Models.Catalog;
using AIAPI.Services;

namespace AIAPI.Services.Implements;

public class QueryEntityExtractor
{
    private static readonly Regex MillionRegex = new(@"(?<value>\d+(?:[.,]\d+)?)\s*(?:trieu|triệu|m\b|million)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ThousandRegex = new(@"(?<value>\d+(?:[.,]\d+)?)\s*(?:nghin|nghìn|k\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DurationRegex = new(@"(?<value>\d+)\s*(?:ngay|ngày|days?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GroupRegex = new(@"(?<value>\d+)\s*(?:nguoi|người|people|pax)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ParsedQueryDTO Extract(string text, ICatalogStore catalog)
    {
        var normalized = text.ToLowerInvariant();
        var result = new ParsedQueryDTO();

        var city = FindKnownPlace(normalized, catalog.Tours.Select(t => t.City).Concat(catalog.TourismItems.Select(t => t.City)));
        var country = FindKnownPlace(normalized, catalog.Tours.Select(t => t.Country).Concat(catalog.TourismItems.Select(t => t.Country)));

        result.City = city;
        result.Country = country;
        result.MinPrice = ExtractMinBudget(normalized);
        result.MaxPrice = ExtractMaxBudget(normalized);
        result.DurationDays = ExtractDuration(normalized);
        result.GroupSize = ExtractGroupSize(normalized);
        result.StartDate = ExtractMonthHint(normalized);

        return result;
    }

    public ParsedQueryDTO Merge(ParsedQueryDTO parsed, TourConsultationRequestDTO request) => new()
    {
        City = request.City ?? parsed.City,
        Country = request.Country ?? parsed.Country,
        MinPrice = request.MinPrice ?? parsed.MinPrice,
        MaxPrice = request.MaxPrice ?? parsed.MaxPrice,
        StartDate = request.PreferredStartDate ?? parsed.StartDate,
        EndDate = request.PreferredEndDate ?? parsed.EndDate,
        DurationDays = request.DurationDays ?? parsed.DurationDays,
        GroupSize = request.GroupSize ?? parsed.GroupSize,
        CategoryId = request.CategoryId ?? parsed.CategoryId
    };

    public ParsedQueryDTO Merge(ParsedQueryDTO parsed, NaturalLanguageSearchRequestDTO request) => new()
    {
        City = request.City ?? parsed.City,
        Country = request.Country ?? parsed.Country,
        MinPrice = request.MinPrice ?? parsed.MinPrice,
        MaxPrice = request.MaxPrice ?? parsed.MaxPrice,
        StartDate = request.StartDate ?? parsed.StartDate,
        EndDate = request.EndDate ?? parsed.EndDate,
        DurationDays = request.DurationDays ?? parsed.DurationDays,
        GroupSize = request.GroupSize ?? parsed.GroupSize,
        CategoryId = request.CategoryId ?? parsed.CategoryId
    };

    private static string? FindKnownPlace(string text, IEnumerable<string?> candidates)
    {
        var catalogCities = candidates
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var geo = VietnamCityGeoResolver.ResolveFromText(text);
        if (geo != null)
        {
            var catalogMatch = catalogCities.FirstOrDefault(c =>
                VietnameseTextNormalizer.CityEquals(c, geo.DisplayName));
            return catalogMatch ?? geo.DisplayName;
        }

        return catalogCities
            .OrderByDescending(c => c!.Length)
            .FirstOrDefault(c => VietnameseTextNormalizer.ContainsNormalized(text, c!));
    }

    private static long? ExtractMinBudget(string text)
    {
        if (text.Contains("duoi", StringComparison.Ordinal) || text.Contains("dưới", StringComparison.Ordinal) ||
            text.Contains("under", StringComparison.Ordinal) || text.Contains("max", StringComparison.Ordinal))
        {
            return ExtractMoney(text);
        }

        return null;
    }

    private static long? ExtractMaxBudget(string text)
    {
        if (text.Contains("tren", StringComparison.Ordinal) || text.Contains("trên", StringComparison.Ordinal) ||
            text.Contains("over", StringComparison.Ordinal) || text.Contains("from", StringComparison.Ordinal))
        {
            return null;
        }

        if (text.Contains("duoi", StringComparison.Ordinal) || text.Contains("dưới", StringComparison.Ordinal) ||
            text.Contains("under", StringComparison.Ordinal) || text.Contains("budget", StringComparison.Ordinal) ||
            text.Contains("ngan sach", StringComparison.Ordinal) || text.Contains("ngân sách", StringComparison.Ordinal))
        {
            return ExtractMoney(text);
        }

        return null;
    }

    private static long? ExtractMoney(string text)
    {
        var million = MillionRegex.Match(text);
        if (million.Success &&
            decimal.TryParse(million.Groups["value"].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var mValue))
        {
            return (long)(mValue * 1_000_000m);
        }

        var thousand = ThousandRegex.Match(text);
        if (thousand.Success &&
            decimal.TryParse(thousand.Groups["value"].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var kValue))
        {
            return (long)(kValue * 1_000m);
        }

        return null;
    }

    private static int? ExtractDuration(string text)
    {
        var match = DurationRegex.Match(text);
        return match.Success && int.TryParse(match.Groups["value"].Value, out var days) ? days : null;
    }

    private static int? ExtractGroupSize(string text)
    {
        var match = GroupRegex.Match(text);
        return match.Success && int.TryParse(match.Groups["value"].Value, out var size) ? size : null;
    }

    private static DateTime? ExtractMonthHint(string text)
    {
        var monthMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["thang 1"] = 1, ["tháng 1"] = 1, ["january"] = 1,
            ["thang 2"] = 2, ["tháng 2"] = 2, ["february"] = 2,
            ["thang 3"] = 3, ["tháng 3"] = 3, ["march"] = 3,
            ["thang 4"] = 4, ["tháng 4"] = 4, ["april"] = 4,
            ["thang 5"] = 5, ["tháng 5"] = 5, ["may"] = 5,
            ["thang 6"] = 6, ["tháng 6"] = 6, ["june"] = 6,
            ["thang 7"] = 7, ["tháng 7"] = 7, ["july"] = 7,
            ["thang 8"] = 8, ["tháng 8"] = 8, ["august"] = 8,
            ["thang 9"] = 9, ["tháng 9"] = 9, ["september"] = 9,
            ["thang 10"] = 10, ["tháng 10"] = 10, ["october"] = 10,
            ["thang 11"] = 11, ["tháng 11"] = 11, ["november"] = 11,
            ["thang 12"] = 12, ["tháng 12"] = 12, ["december"] = 12
        };

        foreach (var pair in monthMap)
        {
            if (text.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
            {
                var year = DateTime.UtcNow.Month <= pair.Value ? DateTime.UtcNow.Year : DateTime.UtcNow.Year + 1;
                return new DateTime(year, pair.Value, 1);
            }
        }

        return null;
    }
}
