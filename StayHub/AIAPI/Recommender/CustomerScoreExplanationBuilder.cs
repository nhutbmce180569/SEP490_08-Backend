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
            return text.IsVietnamese ? "Phù hợp với tiêu chí 'Đi đâu cũng được' của bạn." : "Matches your 'Anywhere' preference.";

        if (score >= 0.99f)
            return text.IsVietnamese ? $"Tour nằm trọn tại {tour.City}, hoàn toàn khớp với điểm đến {profile.PreferredCity} bạn muốn." : $"Located in {tour.City}, exactly as requested.";

        if (score >= 0.75f)
            return text.IsVietnamese ? $"Lịch trình có đi qua hoặc tham gia hoạt động tại {profile.PreferredCity}." : $"References {profile.PreferredCity} in activities.";

        return text.IsVietnamese ? $"Tour này tổ chức tại {tour.City}, không nằm trong khu vực ưu tiên ({profile.PreferredCity}) của bạn." : $"Organized in {tour.City}, not in your preferred area.";
    }

    private static string ExplainBudget(float score, TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile, IAiLocalizedCopy text)
    {
        if (!profile.MaxBudgetPerPerson.HasValue || !tour.MinPrice.HasValue)
            return text.IsVietnamese ? "Phù hợp vì bạn không gò bó ngân sách." : "No budget constraints specified.";

        var priceText = tour.MinPrice.Value.ToString("N0");
        return score <= 0.01f
            ? (text.IsVietnamese ? $"Giá từ {priceText} VND vượt mức trần bạn đặt ra." : $"Price {priceText} VND exceeds budget.")
            : (text.IsVietnamese ? $"Giá từ {priceText} VND nằm hoàn toàn trong giới hạn ngân sách." : $"Price {priceText} VND is well within budget.");
    }

    private static string ExplainSchedule(float score, TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile, IAiLocalizedCopy text)
    {
        if (!tour.NextDeparture.HasValue)
            return text.IsVietnamese ? "Lịch linh hoạt, sẽ chốt khi bạn đặt tour." : "Flexible schedule, confirmed upon booking.";

        var departure = tour.NextDeparture.Value.ToString("dd/MM/yyyy");
        return score >= 0.99f
            ? (text.IsVietnamese ? $"Khởi hành {departure} nằm ngay trong khoảng ngày bạn rảnh." : $"Departs {departure}, right in your window.")
            : (text.IsVietnamese ? $"Khởi hành {departure} hơi lệch lịch một chút, bạn xem xét nhé." : $"Departs {departure}, slightly outside your window.");
    }

    private static string ExplainInterestSemantic(float score, TourPreferenceQuestionnaireDTO profile, IAiLocalizedCopy text, TourCatalogItem? tour = null)
    {
        if (profile.TravelInterests.Count == 0)
            return text.IsVietnamese ? "Không gò bó sở thích, tour nào cũng có thể trải nghiệm." : "No specific interests, open to all.";

        var hitInterests = new List<string>();
        if (tour != null && !string.IsNullOrWhiteSpace(tour.SearchDocument))
        {
            var doc = tour.SearchDocument.ToLowerInvariant();
            hitInterests = profile.TravelInterests
                .Where(i => doc.Contains(i.ToLowerInvariant()) || (i == "beach" && doc.Contains("island")) || (i == "nature" && (doc.Contains("mountain") || doc.Contains("trek"))) || (i == "relax" && doc.Contains("resort")))
                .Select(i => FormatInterestLabel(i, text))
                .ToList();
        }

        var hitText = hitInterests.Count > 0 ? string.Join(", ", hitInterests) : "";

        if (score >= 0.75f)
            return text.IsVietnamese 
                ? (hitText != "" ? $"Khớp rất mạnh với các sở thích ({hitText}) của bạn." : "Khớp rất mạnh với các Vibe du lịch bạn đang tìm kiếm.")
                : (hitText != "" ? $"Strongly matches your interests ({hitText})." : "Strongly matches your travel vibe.");

        if (score >= 0.45f)
            return text.IsVietnamese 
                ? (hitText != "" ? $"Có một vài điểm nhấn ({hitText}) đúng với sở thích của bạn." : "Có một vài điểm nhấn đúng với sở thích của bạn.")
                : (hitText != "" ? $"Has a few highlights ({hitText}) you might like." : "Has a few highlights you might like.");

        return text.IsVietnamese ? "Tour mang phong cách trải nghiệm mới, có thể thử nếu muốn đổi gió." : "Different style, good for trying something new.";
    }

    private static string ExplainWeather(float score, TourCatalogItem tour, IAiLocalizedCopy text)
    {
        if (score >= 0.75f)
            return text.IsVietnamese ? "Điều kiện thời tiết rất ủng hộ cho các hoạt động ngoài trời trong tour này." : "Great weather, perfect for outdoor activities.";

        if (score <= 0.4f)
            return text.IsVietnamese ? "Có khả năng gặp mưa hoặc thời tiết không quá lý tưởng, bạn nhớ xem dự báo và chuẩn bị ô/áo mưa nhé." : "Might rain or have bad weather, bring an umbrella.";

        return text.IsVietnamese ? "Thời tiết tương đối ổn định, không ảnh hưởng nhiều đến trải nghiệm của bạn." : "Stable weather, minimal impact on itinerary.";
    }

    private static string ExplainAccessibility(float score, TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile, IAiLocalizedCopy text)
    {
        var hasVulnerable = profile.ElderlyCount > 0 || profile.ChildrenCount > 0;
        var doc = tour.SearchDocument?.ToLowerInvariant() ?? "";
        
        var isTrek = doc.Contains("trek") || doc.Contains("leo núi");
        var isDive = doc.Contains("lặn") || doc.Contains("dive");
        
        var activities = new List<string>();
        if (isTrek) activities.Add(text.IsVietnamese ? "trekking/leo núi" : "trekking");
        if (isDive) activities.Add(text.IsVietnamese ? "lặn biển" : "diving");

        var actText = activities.Count > 0 ? string.Join(", ", activities) : "";
        
        if (score >= 0.75f)
            return text.IsVietnamese ? "Hoạt động cực kỳ nhẹ nhàng, phù hợp và an toàn cho mọi lứa tuổi." : "Gentle activities, safe and easy for everyone.";

        if (hasVulnerable && score <= 0.4f)
            return text.IsVietnamese 
                ? (actText != "" ? $"Tour có hoạt động mang tính thử thách ({actText}), hãy cân nhắc kĩ khi đi cùng người lớn tuổi/trẻ em." : "Tour có hoạt động tốn nhiều sức, cân nhắc kĩ nếu có người lớn tuổi/trẻ em.")
                : (actText != "" ? $"Includes challenging activities ({actText}), consider carefully for vulnerable travelers." : "Strenuous activities, consider carefully for vulnerable travelers.");

        return text.IsVietnamese ? "Lịch trình có chút vận động cơ bản (đi bộ tham quan) nhưng nhìn chung vẫn an toàn." : "Some physical activity but generally safe.";
    }

    private static string ExplainCulturalFit(float score, TourCatalogItem tour, TourPreferenceQuestionnaireDTO profile, IAiLocalizedCopy text)
    {
        var hasCultureKeyword = tour.SearchDocument?.ToLowerInvariant().Contains("văn hóa") == true || tour.SearchDocument?.ToLowerInvariant().Contains("di sản") == true || tour.SearchDocument?.ToLowerInvariant().Contains("lịch sử") == true;

        if (score >= 0.75f)
            return text.IsVietnamese 
                ? (hasCultureKeyword ? "Lịch trình tập trung khám phá các giá trị văn hóa, lịch sử và di sản đặc sắc của địa phương." : "Rất đậm đà bản sắc văn hóa địa phương.") 
                : "Rich in local culture and heritage.";

        if (score >= 0.45f)
            return text.IsVietnamese ? "Có đan xen một vài điểm đến văn hóa vào lịch trình nhưng không quá nặng nề." : "Some cultural stops, but not heavy.";

        return text.IsVietnamese ? "Tập trung chủ yếu vào ngắm cảnh tự nhiên hoặc hoạt động vui chơi thay vì văn hóa." : "Focuses more on scenery/activities than culture.";
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
