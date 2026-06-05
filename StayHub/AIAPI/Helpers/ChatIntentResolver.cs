using AIAPI.ML;

namespace AIAPI.Helpers;

public static class ChatIntentResolver
{
    private const float HighConfidenceThreshold = 0.52f;

    private static readonly string[] HowToPrefixes =
    [
        "lam sao", "làm sao", "lam the nao", "làm thế nào", "the nao de", "thế nào để",
        "huong dan", "hướng dẫn", "cach ", "cách ", "lam nhu the nao", "làm như thế nào",
        "how to", "how do i", "how can i", "how does"
    ];

    private static readonly string[] SystemSignals =
    [
        "stayhub", "he thong", "hệ thống", "ung dung", "ứng dụng", "platform",
        "dat tour", "đặt tour", "booking", "thanh toan", "thanh toán", "payment",
        "voucher", "ma giam gia", "mã giảm giá", "hoan tien", "hoàn tiền", "refund",
        "huy tour", "hủy tour", "cancel", "dang nhap", "đăng nhập", "dang ky", "đăng ký",
        "login", "register", "tai khoan", "tài khoản", "wishlist", "yeu thich", "yêu thích",
        "danh sach yeu thich", "tro ly ai", "trợ lý ai", "chatbot", "khao sat", "khảo sát",
        "microservice", "api", "gateway", "auth", "social", "sos", "kien truc", "kiến trúc",
        "ho tro", "hỗ trợ", "support", "lien he", "liên hệ", "nha dieu hanh", "nhà điều hành",
        "tinh nang", "tính năng", "chuc nang", "chức năng", "danh gia", "đánh giá",
        "review", "rating", "la gi", "là gì", "lam gi", "làm gì", "o dau", "ở đâu"
    ];

    private static readonly string[] CultureSignals =
    [
        "van hoa", "văn hóa", "dac san", "đặc sản", "am thuc", "ẩm thực",
        "di tich", "di tích", "heritage", "unesco", "lich su", "lịch sử",
        "phong tuc", "phong tục", "le hoi", "lễ hội", "local food", "culture"
    ];

    private static readonly string[] SearchSignals =
    [
        "tim tour", "tìm tour", "tim kiem tour", "tìm kiếm tour", "search tour",
        "co tour", "có tour", "tour bien", "tour biển", "goi y tour", "gợi ý tour",
        "muon di", "muốn đi", "muon tim tour", "muốn tìm tour"
    ];

    public static string Resolve(string message, string mlIntent, float confidence)
    {
        var normalized = VietnameseTextNormalizer.Normalize(message);

        if (IsHowToQuestion(message, normalized) || LooksLikeDefinitionQuestion(normalized))
        {
            return TourIntents.AskSystem;
        }

        if (ContainsAny(normalized, SystemSignals))
        {
            return TourIntents.AskSystem;
        }

        if (ContainsAny(normalized, CultureSignals))
        {
            return TourIntents.AskCulture;
        }

        if (IsExplicitTourSearch(normalized))
        {
            return TourIntents.SearchTour;
        }

        if (confidence >= HighConfidenceThreshold &&
            mlIntent is not (TourIntents.Unknown or TourIntents.RecommendTour))
        {
            return mlIntent;
        }

        if (confidence >= HighConfidenceThreshold && mlIntent != TourIntents.Unknown)
        {
            return mlIntent;
        }

        return TourIntents.AskSystem;
    }

    public static bool LooksLikeSystemQuestion(string message)
    {
        var normalized = VietnameseTextNormalizer.Normalize(message);
        return IsHowToQuestion(message, normalized) ||
               LooksLikeDefinitionQuestion(normalized) ||
               ContainsAny(normalized, SystemSignals);
    }

    public static bool IsHowToQuestion(string message, string? normalized = null) =>
        IsHowToNormalized(normalized ?? VietnameseTextNormalizer.Normalize(message));

    private static bool IsHowToNormalized(string normalized)
    {
        if (!ContainsAny(normalized, HowToPrefixes))
        {
            return false;
        }

        return normalized.Contains("stayhub", StringComparison.Ordinal) ||
               normalized.Contains("hethong", StringComparison.Ordinal) ||
               normalized.Contains("dattour", StringComparison.Ordinal) ||
               normalized.Contains("booking", StringComparison.Ordinal) ||
               normalized.Contains("thanhtoan", StringComparison.Ordinal) ||
               normalized.Contains("payment", StringComparison.Ordinal) ||
               normalized.Contains("voucher", StringComparison.Ordinal) ||
               normalized.Contains("huytour", StringComparison.Ordinal) ||
               normalized.Contains("hoantien", StringComparison.Ordinal) ||
               normalized.Contains("cancel", StringComparison.Ordinal) ||
               normalized.Contains("dangnhap", StringComparison.Ordinal) ||
               normalized.Contains("dangky", StringComparison.Ordinal) ||
               normalized.Contains("login", StringComparison.Ordinal) ||
               normalized.Contains("register", StringComparison.Ordinal) ||
               normalized.Contains("taikhoan", StringComparison.Ordinal) ||
               normalized.Contains("wishlist", StringComparison.Ordinal) ||
               normalized.Contains("khaosat", StringComparison.Ordinal) ||
               normalized.Contains("trolyai", StringComparison.Ordinal) ||
               normalized.Contains("danhgia", StringComparison.Ordinal) ||
               normalized.Contains("review", StringComparison.Ordinal) ||
               normalized.Contains("nhadieuhanh", StringComparison.Ordinal) ||
               normalized.Contains("lienhe", StringComparison.Ordinal) ||
               normalized.Contains("hotro", StringComparison.Ordinal) ||
               normalized.Contains("social", StringComparison.Ordinal);
    }

    private static bool LooksLikeDefinitionQuestion(string normalized) =>
        normalized.Contains("lagi", StringComparison.Ordinal) ||
        normalized.Contains("làgì", StringComparison.Ordinal) ||
        normalized.Contains("lanhuong", StringComparison.Ordinal) ||
        normalized.Contains("whatis", StringComparison.Ordinal) ||
        normalized.Contains("whatcan", StringComparison.Ordinal);

    private static bool IsExplicitTourSearch(string normalized)
    {
        if (IsHowToNormalized(normalized) || LooksLikeDefinitionQuestion(normalized))
        {
            return false;
        }

        if (ContainsAny(normalized, SearchSignals))
        {
            return true;
        }

        if (!normalized.Contains("tour", StringComparison.Ordinal))
        {
            return false;
        }

        return VietnamCityGeoResolver.ResolveFromText(normalized) != null;
    }

    private static bool ContainsAny(string normalized, IEnumerable<string> signals) =>
        signals.Any(s => normalized.Contains(VietnameseTextNormalizer.Normalize(s), StringComparison.Ordinal));
}
