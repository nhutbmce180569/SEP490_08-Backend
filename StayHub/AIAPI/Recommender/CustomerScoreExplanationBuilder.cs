using AIAPI.DTOs;
using AIAPI.Helpers;
using AIAPI.Localization;
using AIAPI.Models.Catalog;

namespace AIAPI.Recommender;

public static class CustomerScoreExplanationBuilder
{
    public static List<ScoreDimensionExplanationDTO> Build(
        TourCatalogItem tour,
        TourPreferenceQuestionnaireDTO profile,
        Dictionary<string, float> dimensionScores,
        IDimensionWeightProvider weights,
        IAiLocalizedCopy text)
    {
        var weightMap = weights.AsDictionary();
        var items = dimensionScores
            .Select(kv => new ScoreDimensionExplanationDTO
            {
                DimensionKey = kv.Key,
                Label = FormatDimensionLabel(kv.Key, text),
                Score = kv.Value,
                Weight = weightMap.TryGetValue(kv.Key, out var w) ? w : 0f,
                Explanation = ExplainDimension(kv.Key, kv.Value, tour, profile, text)
            })
            .OrderByDescending(x => x.Score * x.Weight)
            .ThenByDescending(x => x.Score)
            .ToList();

        return items;
    }

    private static string FormatDimensionLabel(string key, IAiLocalizedCopy text) => key switch
    {
        "interest_semantic" => text.IsVietnamese ? "Sở thích du lịch" : "Travel interests",
        "location" => text.IsVietnamese ? "Điểm đến" : "Destination",
        "budget" => text.IsVietnamese ? "Ngân sách" : "Budget",
        "schedule" => text.IsVietnamese ? "Lịch khởi hành" : "Departure schedule",
        "weather" => text.IsVietnamese ? "Thời tiết dự kiến" : "Expected weather",
        "accessibility" => text.IsVietnamese ? "Độ dễ đi / an toàn" : "Ease & safety",
        "cultural_fit" => text.IsVietnamese ? "Văn hóa địa phương" : "Local culture fit",
        _ => key.Replace('_', ' ')
    };

    private static string ExplainDimension(
        string key,
        float score,
        TourCatalogItem tour,
        TourPreferenceQuestionnaireDTO profile,
        IAiLocalizedCopy text)
    {
        return key switch
        {
            "location" => ExplainLocation(score, tour, profile, text),
            "budget" => ExplainBudget(score, tour, profile, text),
            "schedule" => ExplainSchedule(score, tour, profile, text),
            "interest_semantic" => ExplainInterestSemantic(score, profile, text, tour),
            "weather" => ExplainWeather(score, tour, text),
            "accessibility" => ExplainAccessibility(score, tour, profile, text),
            "cultural_fit" => ExplainCulturalFit(score, tour, profile, text),
            _ => text.IsVietnamese
                ? $"Chỉ số này đạt {FormatPercent(score)} dựa trên thông tin tour và khảo sát của bạn."
                : $"This factor scores {FormatPercent(score)} based on the tour details and your survey answers."
        };
    }

    private static string ExplainLocation(float score, TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile, IAiLocalizedCopy text)
    {
        if (string.IsNullOrWhiteSpace(profile.PreferredCity))
        {
            return text.IsVietnamese
                ? "Bạn chưa chọn điểm đến cụ thể nên chỉ số này ở mức trung bình (50%) — tour vẫn được xét theo sở thích khác."
                : "You did not pick a specific destination, so this stays at a neutral 50% — other factors still drive the match.";
        }

        if (score >= 0.99f)
        {
            return text.IsVietnamese
                ? $"Tour tại {tour.City} — khớp hoàn toàn với điểm đến bạn chọn ({profile.PreferredCity})."
                : $"Tour is in {tour.City}, exactly matching your preferred destination ({profile.PreferredCity}).";
        }

        if (score >= 0.7f)
        {
            return text.IsVietnamese
                ? $"Tour có liên quan đến {profile.PreferredCity} trong tên hoặc mô tả (khớp {FormatPercent(score)})."
                : $"The tour references {profile.PreferredCity} in its name or description ({FormatPercent(score)} match).";
        }

        return text.IsVietnamese
            ? $"Tour không nằm tại {profile.PreferredCity} bạn đã chọn nên chỉ số điểm đến thấp ({FormatPercent(score)})."
            : $"The tour is not in your chosen area ({profile.PreferredCity}), so the destination score is low ({FormatPercent(score)}).";
    }

    private static string ExplainBudget(float score, TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile, IAiLocalizedCopy text)
    {
        if (!profile.MaxBudgetPerPerson.HasValue || !tour.MinPrice.HasValue)
        {
            return text.IsVietnamese
                ? "Bạn chưa khai báo ngân sách hoặc tour chưa có giá — chỉ số mặc định 50%."
                : "Budget or tour price is missing — this factor defaults to 50%.";
        }

        var price = tour.MinPrice.Value;
        var budget = profile.MaxBudgetPerPerson.Value;
        var priceText = price.ToString("N0");
        var budgetText = budget.ToString("N0");

        if (score <= 0.01f)
        {
            return text.IsVietnamese
                ? $"Giá từ {priceText} VND vượt ngân sách {budgetText} VND/người bạn đã khai báo."
                : $"Price from {priceText} VND exceeds your {budgetText} VND/person budget.";
        }

        if (score >= 0.85f)
        {
            return text.IsVietnamese
                ? $"Giá từ {priceText} VND nằm thoải mái trong ngân sách {budgetText} VND/người ({FormatPercent(score)} khớp)."
                : $"Price from {priceText} VND sits comfortably within your {budgetText} VND/person budget ({FormatPercent(score)} fit).";
        }

        return text.IsVietnamese
            ? $"Giá từ {priceText} VND gần với ngân sách {budgetText} VND/người — vẫn trong giới hạn nhưng ít dư hơn ({FormatPercent(score)})."
            : $"Price from {priceText} VND is close to your {budgetText} VND/person cap — still within budget but with less headroom ({FormatPercent(score)}).";
    }

    private static string ExplainSchedule(float score, TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile, IAiLocalizedCopy text)
    {
        if (!tour.NextDeparture.HasValue)
        {
            return text.IsVietnamese
                ? "Tour chưa có lịch khởi hành cụ thể nên chỉ số lịch trình ở mức thấp-trung bình (40%)."
                : "No confirmed departure date yet, so the schedule score stays at a modest 40%.";
        }

        var departure = tour.NextDeparture.Value.ToString("dd/MM/yyyy");
        var windowStart = profile.PreferredStartDate.Date;
        var windowEnd = (profile.PreferredEndDate ?? profile.PreferredStartDate.AddDays(30)).Date;

        if (score >= 0.99f)
        {
            return text.IsVietnamese
                ? $"Khởi hành {departure} nằm trong khoảng ngày bạn chọn ({windowStart:dd/MM/yyyy} → {windowEnd:dd/MM/yyyy})."
                : $"Departure on {departure} falls inside your travel window ({windowStart:dd/MM/yyyy} → {windowEnd:dd/MM/yyyy}).";
        }

        return text.IsVietnamese
            ? $"Khởi hành {departure} ngoài khoảng ngày bạn chọn — vẫn có thể cân nhắc nếu linh hoạt lịch ({FormatPercent(score)})."
            : $"Departure on {departure} is outside your selected dates — worth considering if your dates are flexible ({FormatPercent(score)}).";
    }

    private static string ExplainInterestSemantic(
        float score,
        TourPreferenceQuestionnaireDTO profile,
        IAiLocalizedCopy text,
        TourCatalogItem? tour = null)
    {
        var total = profile.TravelInterests.Count;
        var matchedKeys = tour != null
            ? InterestMatchHelper.GetMatchedInterestKeys(tour.SearchDocument, profile.TravelInterests)
            : [];
        var matchedLabels = matchedKeys.Select(i => FormatInterestLabel(i, text)).ToList();
        var joined = string.Join(", ", profile.TravelInterests.Select(i => FormatInterestLabel(i, text)));

        if (total > 0 && matchedKeys.Count > 0)
        {
            var matchedText = string.Join(", ", matchedLabels);
            return text.IsVietnamese
                ? $"Khớp {matchedKeys.Count}/{total} sở thích bạn chọn ({matchedText}). Điểm tổng hợp từ khớp từ khóa + AI semantic: {FormatPercent(score)}."
                : $"Matches {matchedKeys.Count}/{total} interests you selected ({matchedText}). Combined keyword + semantic AI score: {FormatPercent(score)}.";
        }

        if (score >= 0.75f)
        {
            return text.IsVietnamese
                ? $"Nội dung tour khớp mạnh với sở thích bạn chọn ({joined}) — {FormatPercent(score)}."
                : $"Tour content strongly aligns with your interests ({joined}) — {FormatPercent(score)}.";
        }

        if (score >= 0.45f)
        {
            return text.IsVietnamese
                ? $"Tour có phần phù hợp sở thích ({joined}) nhưng chưa khớp hoàn toàn ({FormatPercent(score)})."
                : $"The tour partially matches your interests ({joined}) but not perfectly ({FormatPercent(score)}).";
        }

        if (total > 0)
        {
            return text.IsVietnamese
                ? $"Bạn chọn {total} sở thích ({joined}) nhưng mô tả tour chưa chứa từ khóa tương ứng — chỉ số {FormatPercent(score)}."
                : $"You selected {total} interests ({joined}) but the tour description lacks matching keywords — score {FormatPercent(score)}.";
        }

        return text.IsVietnamese
            ? $"Nội dung tour ít liên quan đến sở thích bạn chọn — chỉ số {FormatPercent(score)}."
            : $"Tour content has limited overlap with your interests — score {FormatPercent(score)}.";
    }

    private static string ExplainWeather(float score, TourCatalogItem tour, IAiLocalizedCopy text)
    {
        var doc = tour.SearchDocument.ToLowerInvariant();
        var isBeach = doc.Contains("beach") || doc.Contains("island") || doc.Contains("biển");

        if (score >= 0.85f)
        {
            return text.IsVietnamese
                ? "Thời tiết dự kiến thuận lợi cho loại hình tour này (ít mưa, phù hợp hoạt động ngoài trời)."
                : "Forecast weather suits this tour type (low rain, good for outdoor activities).";
        }

        if (score <= 0.35f && isBeach)
        {
            return text.IsVietnamese
                ? "Dự báo nhiều mưa trong khoảng ngày đi — tour biển/đảo có thể bị ảnh hưởng."
                : "Rain is expected during your dates — beach/island tours may be affected.";
        }

        if (score >= 0.55f)
        {
            return text.IsVietnamese
                ? "Thời tiết ở mức chấp nhận được cho tour này — không quá thuận lợi nhưng vẫn đi được."
                : "Weather is acceptable for this tour — not ideal, but still workable.";
        }

        return text.IsVietnamese
            ? $"Thời tiết ảnh hưởng vừa phải đến trải nghiệm tour ({FormatPercent(score)})."
            : $"Weather has a moderate impact on this tour experience ({FormatPercent(score)}).";
    }

    private static string ExplainAccessibility(float score, TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile, IAiLocalizedCopy text)
    {
        var doc = tour.SearchDocument.ToLowerInvariant();
        var strenuous = doc.Contains("trek") || doc.Contains("motorbike") || doc.Contains("climb");
        var gentle = doc.Contains("cruise") || doc.Contains("garden") || doc.Contains("resort") || doc.Contains("floating market");

        if (profile.HasElderly || profile.HasChildren)
        {
            if (score >= 0.75f)
            {
                return text.IsVietnamese
                    ? "Lịch trình nhẹ nhàng, phù hợp khi đi cùng người cao tuổi hoặc trẻ em."
                    : "Relaxed pace — suitable when traveling with elderly or children.";
            }

            if (strenuous)
            {
                return text.IsVietnamese
                    ? "Tour có hoạt động mạnh (trek/xe máy/leo núi) — cần cân nhắc nếu có người già hoặc trẻ nhỏ."
                    : "Includes strenuous activities (trek/motorbike/climb) — consider carefully with elderly or young kids.";
            }
        }

        if (gentle && score >= 0.8f)
        {
            return text.IsVietnamese
                ? "Tour có hoạt động nhẹ nhàng (du thuyền, tham quan, nghỉ dưỡng) — dễ tham gia cho mọi lứa tuổi."
                : "Gentle activities (cruise, sightseeing, resort) — easy for most travelers.";
        }

        if (strenuous)
        {
            return text.IsVietnamese
                ? "Tour thiên về mạo hiểm/vận động nhiều nên chỉ số dễ đi thấp hơn."
                : "Adventure-heavy itinerary lowers the ease-of-travel score.";
        }

        return text.IsVietnamese
            ? $"Mức độ dễ đi và an toàn của tour: {FormatPercent(score)}."
            : $"Ease and safety rating for this tour: {FormatPercent(score)}.";
    }

    private static string ExplainCulturalFit(float score, TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile, IAiLocalizedCopy text)
    {
        var wantsCulture = profile.TravelInterests.Contains("culture", StringComparer.OrdinalIgnoreCase)
            || profile.NationalityType == TravelerNationalityTypes.Foreigner;

        if (!wantsCulture && score >= 0.45f && score <= 0.55f)
        {
            return text.IsVietnamese
                ? "Bạn không ưu tiên văn hóa địa phương nên chỉ số này ở mức trung bình (50%)."
                : "Local culture is not a top priority for you, so this stays neutral (50%).";
        }

        if (score >= 0.8f)
        {
            return text.IsVietnamese
                ? $"Tour tại {tour.City} có nhiều yếu tố văn hóa/di sản phù hợp nhu cầu tìm hiểu địa phương ({FormatPercent(score)})."
                : $"Tour in {tour.City} offers strong cultural/heritage experiences ({FormatPercent(score)}).";
        }

        if (score >= 0.55f)
        {
            return text.IsVietnamese
                ? "Có một số điểm văn hóa trong hành trình nhưng chưa phải trọng tâm chính."
                : "Some cultural stops are included, but culture is not the main focus.";
        }

        return text.IsVietnamese
            ? $"Mức phù hợp văn hóa địa phương: {FormatPercent(score)}."
            : $"Local culture fit: {FormatPercent(score)}.";
    }

    private static string FormatInterestLabel(string key, IAiLocalizedCopy text) => key.ToLowerInvariant() switch
    {
        "beach" => text.IsVietnamese ? "biển/đảo" : "beach/islands",
        "culture" => text.IsVietnamese ? "văn hóa" : "culture",
        "nature" => text.IsVietnamese ? "thiên nhiên" : "nature",
        "food" => text.IsVietnamese ? "ẩm thực" : "food",
        "adventure" => text.IsVietnamese ? "mạo hiểm" : "adventure",
        "relax" => text.IsVietnamese ? "nghỉ dưỡng" : "relaxation",
        "photography" => text.IsVietnamese ? "chụp ảnh" : "photography",
        "city" => text.IsVietnamese ? "thành phố" : "city",
        "river" => text.IsVietnamese ? "sông nước" : "rivers",
        _ => key
    };

    private static string FormatPercent(float score) => $"{Math.Round(Math.Clamp(score, 0f, 1f) * 100)}%";
}
