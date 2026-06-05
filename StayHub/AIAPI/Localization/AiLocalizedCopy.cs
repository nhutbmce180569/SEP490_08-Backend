using AIAPI.Recommender;

namespace AIAPI.Localization;

public interface IAiLocalizedCopy
{
    bool IsVietnamese { get; }

    // Questionnaire
    string QuestionCompanionType { get; }
    string QuestionCompanionHint { get; }
    string OptionSolo { get; }
    string OptionCouple { get; }
    string OptionFamily { get; }
    string OptionGroup { get; }
    string QuestionStartDate { get; }
    string QuestionStartDateHint { get; }
    string QuestionEndDate { get; }
    string QuestionBudget { get; }
    string QuestionHasElderly { get; }
    string QuestionHasChildren { get; }
    string QuestionInterests { get; }
    string OptionBeach { get; }
    string OptionCulture { get; }
    string OptionNature { get; }
    string OptionFood { get; }
    string OptionAdventure { get; }
    string OptionRelax { get; }
    string OptionPhotography { get; }
    string OptionCity { get; }
    string OptionRiver { get; }
    string QuestionNationality { get; }
    string OptionVietnamese { get; }
    string OptionForeigner { get; }
    string QuestionPreferredCity { get; }
    string QuestionPreferredCityHint { get; }

    // Personas
    string PersonaPrimary { get; }
    string PersonaElderly { get; }
    string PersonaChild { get; }
    string PersonaInternational { get; }
    string PersonaCouple { get; }
    string PersonaFamily { get; }
    string PersonaGroup { get; }

    // Match reasons
    string ReasonElderlyGood(string persona, float score);
    string ReasonElderlyPoor(string persona);
    string ReasonChildGood(string persona, float score);
    string ReasonChildPoor(string persona);
    string ReasonInternationalGood(string persona);
    string ReasonInterestMatch(string persona, IEnumerable<string> interests);
    string ReasonLocationMatch(string city);
    string ReasonBudgetFit { get; }
    string ReasonInterestStrong { get; }
    string ReasonScheduleFit { get; }
    string ReasonGoodRating { get; }

    // Summary & tips
    string SummaryNoTours { get; }
    string SummaryFound(int personaCount, int tourCount, string topScore, string? weatherNote);
    string TipFairnessModel { get; }
    string TipChildren1 { get; }
    string TipChildren2 { get; }

    // Chat
    string ChatGreeting { get; }
    IReadOnlyList<string> ChatDefaultSuggestions { get; }
    string ChatCultureSuggest1 { get; }
    string ChatCultureSuggest2 { get; }
    string ChatCultureSuggest3 { get; }
    string ChatToursFound(int count);
    string ChatNoTours { get; }
    string ChatPersonalizedLoggedIn { get; }
    string ChatPersonalizedAnonymous { get; }
    string ChatCultureNoData { get; }
    string ChatCultureNoDataForCity(string city);
    string ChatCultureReply(string source, string name, string type, string description);

    // Schedule
    string ScheduleExactMatch(DateTime departure);
    string ScheduleNearbyBefore(DateTime departure, int days);
    string ScheduleNearbyAfter(DateTime departure, int days);
    string ScheduleUnknownDeparture { get; }
    string ScheduleNoExactButNearby { get; }
    string ScheduleNoExactFallback { get; }
    string ScheduleHasExact(int count);
    string SchedulePartialExactAndNearby(int exactCount, int alternateCount);
    string ScheduleExtendedBefore(DateTime departure, int days);
    string ScheduleExtendedAfter(DateTime departure, int days);

    // Recommendations
    string ReasonWishlistHistory { get; }
    string ReasonPopularTour { get; }
    string ReasonSimilarTour(string tourName);

    // Foreign visitors
    string ForeignVisitorGeneralHeader { get; }
    IReadOnlyList<string> ForeignVisitorGeneralDosAndDonts { get; }
    string ForeignVisitorDestinationHeader(string city);

    // Weather
    string WeatherForecastSummary(string place, double min, double max, double rain, int days);
    string WeatherHistoricalSummary(string place, double min, double max, double rain);
    string WeatherImpactRainy { get; }
    string WeatherImpactHot { get; }
    string WeatherImpactCool { get; }
    string WeatherImpactMild { get; }
}

public sealed class AiLocalizedCopy : IAiLocalizedCopy
{
    private readonly bool _vi;

    public AiLocalizedCopy(IAiCultureAccessor culture) => _vi = culture.IsVietnamese;

    public bool IsVietnamese => _vi;

    public string QuestionCompanionType => T("Who are you traveling with?", "Bạn đi tour cùng ai?");
    public string QuestionCompanionHint => T(
        "We tailor suggestions for everyone in your group — including elderly and children.",
        "Chúng tôi gợi ý tour phù hợp với từng người trong nhóm — kể cả người già và trẻ em.");
    public string OptionSolo => T("Solo", "Một mình");
    public string OptionCouple => T("Couple", "Couple / đôi");
    public string OptionFamily => T("Family", "Gia đình");
    public string OptionGroup => T("Friends group", "Nhóm bạn");
    public string QuestionStartDate => T("Planned start date?", "Dự kiến đi từ ngày nào?");
    public string QuestionStartDateHint => T(
        "≤14 days: Open-Meteo forecast. Further out: historical data for the same period.",
        "≤14 ngày: Open-Meteo forecast. Xa hơn: dữ liệu lịch sử cùng kỳ.");
    public string QuestionEndDate => T("Planned end date?", "Dự kiến đến ngày nào?");
    public string QuestionBudget => T("Max budget per person (VND)?", "Ngân sách tối đa mỗi người (VND)?");
    public string QuestionHasElderly => T("Any elderly travelers?", "Có người cao tuổi đi cùng không?");
    public string QuestionHasChildren => T("Any children traveling?", "Có trẻ em đi cùng không?");
    public string QuestionInterests => T("What types of travel do you enjoy?", "Bạn thích loại hình du lịch nào?");
    public string OptionBeach => T("Beach / islands", "Biển / đảo");
    public string OptionCulture => T("Culture / heritage", "Văn hóa / di sản");
    public string OptionNature => T("Nature", "Thiên nhiên");
    public string OptionFood => T("Food", "Ẩm thực");
    public string OptionAdventure => T("Adventure", "Mạo hiểm");
    public string OptionRelax => T("Relaxation", "Nghỉ dưỡng");
    public string OptionPhotography => T("Photography", "Chụp ảnh");
    public string OptionCity => T("City exploration", "Khám phá thành phố");
    public string OptionRiver => T("Rivers / Mekong Delta", "Sông nước / miền Tây");
    public string QuestionNationality => T("Vietnamese or international guest?", "Bạn là khách Việt Nam hay quốc tế?");
    public string OptionVietnamese => T("Vietnamese", "Người Việt Nam");
    public string OptionForeigner => T("International guest", "Khách quốc tế");
    public string QuestionPreferredCity => T("Where do you want to go? (optional)", "Muốn đi đâu? (tùy chọn)");
    public string QuestionPreferredCityHint => T("e.g. Can Tho, Hoi An…", "Ví dụ: Can Tho, Hoi An...");

    public string PersonaPrimary => T("Primary traveler", "Du khách chính");
    public string PersonaElderly => T("Elderly companion", "Người cao tuổi đi cùng");
    public string PersonaChild => T("Child companion", "Trẻ em đi cùng");
    public string PersonaInternational => T("International guest", "Khách quốc tế");
    public string PersonaCouple => T("Couple vibe", "Không khí couple");
    public string PersonaFamily => T("Family vibe", "Không khí gia đình");
    public string PersonaGroup => T("Group vibe", "Không khí nhóm");

    public string ReasonElderlyGood(string persona, float score) => _vi
        ? "Lịch trình nhẹ nhàng, dễ đi cùng người lớn tuổi."
        : "A relaxed pace that works well with elderly travelers.";

    public string ReasonElderlyPoor(string persona) => _vi
        ? "Có thể hơi mệt cho người cao tuổi (đi bộ nhiều hoặc hoạt động mạnh)."
        : "May be tiring for elderly travelers (lots of walking or strenuous activities).";

    public string ReasonChildGood(string persona, float score) => _vi
        ? "Có hoạt động phù hợp cho trẻ em đi cùng."
        : "Includes activities that work well for children.";

    public string ReasonChildPoor(string persona) => _vi
        ? "Nên cân nhắc nếu đi cùng trẻ nhỏ (tour dài hoặc mạo hiểm)."
        : "Worth a closer look if traveling with young kids (long days or adventure-heavy).";

    public string ReasonInternationalGood(string persona) => _vi
        ? "Dễ tham gia và tìm hiểu văn hóa — phù hợp khách quốc tế."
        : "Easy to follow and explore local culture — good for international visitors.";

    public string ReasonInterestMatch(string persona, IEnumerable<string> interests)
    {
        var labels = interests.Select(FormatInterestLabel).ToList();
        return _vi
            ? $"Phù hợp sở thích của bạn: {string.Join(", ", labels)}."
            : $"Matches what you enjoy: {string.Join(", ", labels)}.";
    }

    public string ReasonLocationMatch(string city) => _vi
        ? $"Đúng khu vực bạn muốn đến: {city}."
        : $"In the area you asked for: {city}.";

    public string ReasonBudgetFit => _vi
        ? "Giá tour nằm trong ngân sách bạn đã khai báo."
        : "Priced within the budget you shared.";

    public string ReasonInterestStrong => _vi
        ? "Nội dung tour khớp với sở thích du lịch của bạn."
        : "Tour content aligns with your travel interests.";

    public string ReasonScheduleFit => _vi
        ? "Có lịch khởi hành phù hợp thời gian bạn dự định đi."
        : "Has a departure date that fits your travel window.";

    public string ReasonGoodRating => _vi
        ? "Tour được đánh giá tốt từ khách đã đi trước đó."
        : "Well rated by travelers who joined before.";

    public string SummaryNoTours => T(
        "No tours satisfy the hard constraints. Try increasing your budget or changing the destination.",
        "Không có tour thỏa ràng buộc cứng. Thử nới ngân sách hoặc đổi điểm đến.");

    public string SummaryFound(int personaCount, int tourCount, string topScore, string? weatherNote) => _vi
        ? $"Dựa trên sở thích và người đi cùng bạn, chúng tôi tìm thấy {tourCount} tour phù hợp — tour khớp nhất đạt {topScore}.{weatherNote ?? ""}"
        : $"Based on your preferences and travel party, we found {tourCount} matching tour(s) — best fit {topScore}.{weatherNote ?? ""}";

    public string TipFairnessModel => _vi
        ? "Gợi ý cân bằng sở thích của mọi người trong nhóm — không ai bị bỏ qua hoàn toàn."
        : "Suggestions balance everyone's preferences in your group — no traveler is completely left out.";

    public string TipChildren1 => T(
        "Prefer tours with interactive activities and reasonable rest time for children.",
        "Ưu tiên tour có hoạt động tương tác, thời gian nghỉ hợp lý cho trẻ em.");

    public string TipChildren2 => T(
        "Check child ticket prices before booking.",
        "Kiểm tra giá vé trẻ em trước khi đặt.");

    public string ChatGreeting => T(
        "Hello! I'm StayHub's AI assistant, trained with ML.NET on real tour data. Ask about tours, budget, destinations, or local culture.",
        "Xin chào! Tôi là trợ lý AI của StayHub, được huấn luyện bằng ML.NET trên dữ liệu tour thật. Bạn có thể hỏi về tour, ngân sách, điểm đến hoặc văn hóa địa phương.");

    public IReadOnlyList<string> ChatDefaultSuggestions => _vi
        ? ["Gợi ý tour biển giá dưới 5 triệu", "Tour Đà Lạt 3 ngày tháng 7", "Đặc sản và văn hóa Hội An"]
        : ["Beach tours under 5M VND", "3-day Da Lat tour in July", "Hoi An food and culture"];

    public string ChatCultureSuggest1 => T("Suggest tours for this destination", "Gợi ý tour phù hợp tại đây");
    public string ChatCultureSuggest2 => T("Budget-friendly tours here", "Tour giá rẻ cho điểm đến này");
    public string ChatCultureSuggest3 => T("Sample 3-day itinerary", "Lịch trình mẫu 3 ngày");

    public string ChatToursFound(int count) => _vi
        ? $"Tôi tìm thấy {count} tour phù hợp với yêu cầu của bạn (semantic search ML.NET)."
        : $"I found {count} tours matching your request (ML.NET semantic search).";

    public string ChatNoTours => T(
        "No tour matched perfectly. Try relaxing your budget or changing the city/dates.",
        "Chưa có tour khớp hoàn toàn. Bạn thử nới ngân sách hoặc đổi thành phố/ thời gian khác nhé.");

    public string ChatPersonalizedLoggedIn => T(
        "Here are personalized tours based on your wishlist, booking history, and our ML models.",
        "Đây là các tour được gợi ý cá nhân hóa dựa trên wishlist, lịch sử đặt tour và mô hình ML nội bộ.");

    public string ChatPersonalizedAnonymous => T(
        "Sign in for better personalization. For now, suggestions use popularity and relevance.",
        "Đăng nhập để nhận gợi ý cá nhân hóa tốt hơn. Hiện tại tôi gợi ý theo độ phổ biến và mức độ liên quan.");

    public string ChatCultureNoData => T(
        "I don't have authoritative reference data for that question. Try asking about a specific city.",
        "Tôi chưa có dữ liệu tham khảo chính thống khớp câu hỏi. Bạn thử hỏi theo tên thành phố cụ thể.");

    public string ChatCultureNoDataForCity(string city) => _vi
        ? $"Chưa có bài viết tham khảo active cho {city}. Bạn có thể xem các tour liên quan bên dưới."
        : $"No active reference articles for {city}. See related tours below.";

    public string ChatCultureReply(string source, string name, string type, string description) => _vi
        ? $"Theo {source}, {name} ({type}): {description}"
        : $"According to {source}, {name} ({type}): {description}";

    public string ScheduleExactMatch(DateTime departure) => _vi
        ? $"Khởi hành {departure:dd/MM/yyyy} — nằm trong khoảng thời gian bạn chọn."
        : $"Departs {departure:yyyy-MM-dd} — within your preferred dates.";

    public string ScheduleNearbyBefore(DateTime departure, int days) => _vi
        ? $"Khởi hành {departure:dd/MM/yyyy} — sớm hơn {days} ngày so với khoảng bạn chọn."
        : $"Departs {departure:yyyy-MM-dd} — {days} day(s) before your preferred window.";

    public string ScheduleNearbyAfter(DateTime departure, int days) => _vi
        ? $"Khởi hành {departure:dd/MM/yyyy} — muộn hơn {days} ngày so với khoảng bạn chọn."
        : $"Departs {departure:yyyy-MM-dd} — {days} day(s) after your preferred window.";

    public string ScheduleUnknownDeparture => T(
        "Departure date will be confirmed when you book — tour matches your interests and budget.",
        "Lịch khởi hành sẽ xác nhận khi đặt tour — tour vẫn phù hợp sở thích và ngân sách của bạn.");

    public string ScheduleNoExactButNearby => T(
        "No departures fall exactly within your travel dates, but these tours leave on nearby dates and still match your preferences.",
        "Không có lịch khởi hành trùng khoảng thời gian bạn chọn, nhưng các tour dưới đây có ngày gần đó và vẫn phù hợp sở thích của bạn.");

    public string ScheduleNoExactFallback => T(
        "No departures match your exact dates. These are the closest available options based on your preferences.",
        "Không có tour khởi hành đúng khoảng thời gian bạn chọn. Dưới đây là các lựa chọn gần nhất theo sở thích của bạn.");

    public string ScheduleHasExact(int count) => _vi
        ? $"Có {count} tour khởi hành trong khoảng thời gian bạn chọn."
        : $"{count} tour(s) depart within your preferred travel window.";

    public string ForeignVisitorGeneralHeader => _vi
        ? "Quy tắc chung khi đến Việt Nam (nên tránh):"
        : "General rules for visiting Vietnam (please avoid):";

    public IReadOnlyList<string> ForeignVisitorGeneralDosAndDonts => _vi
        ?
        [
            "Không chụp ảnh cơ quan nhà nước, quân đội hoặc người dân nếu chưa xin phép.",
            "Không chỉ tay vào người khác, đặc biệt người lớn tuổi; không vuốt đầu trẻ em.",
            "Không mặc trang phục hở khi vào chùa, đình hoặc nhà dân.",
            "Không đưa đồ ăn bằng đũa đang dùng sang bát/người khác; không cắm đũa thẳng vào bát cơm.",
            "Không tranh cãi to tiếng nơi công cộng; giữ giọng nhẹ nhàng khi giao tiếp.",
            "Không dùng ma túy; tuân thủ luật giao thông và mang theo hộ chiếu/visa khi di chuyển."
        ]
        :
        [
            "Do not photograph government sites, military areas, or locals without permission.",
            "Do not point at people, touch children's heads, or pat elders without consent.",
            "Do not wear revealing clothing at temples, shrines, or local homes.",
            "Do not pass food with your personal chopsticks; do not stick chopsticks upright in rice.",
            "Avoid loud arguments in public; keep a calm, respectful tone.",
            "Do not use illegal drugs; follow traffic laws and carry passport/visa when traveling."
        ];

    public string ForeignVisitorDestinationHeader(string city) => _vi
        ? $"Lưu ý riêng tại {city}:"
        : $"Destination-specific tips for {city}:";

    public string SchedulePartialExactAndNearby(int exactCount, int alternateCount) => _vi
        ? $"Có {exactCount} tour khớp lịch bạn chọn; thêm {alternateCount} tour phù hợp với lịch khởi hành gần hoặc xa hơn một chút."
        : $"{exactCount} tour(s) match your dates; plus {alternateCount} more with nearby or slightly later departures.";

    public string ScheduleExtendedBefore(DateTime departure, int days) => _vi
        ? $"Khởi hành {departure:dd/MM/yyyy} — sớm hơn {days} ngày (ngoài ±30 ngày, vẫn trong phạm vi gợi ý)."
        : $"Departs {departure:yyyy-MM-dd} — {days} day(s) earlier (beyond ±30 days, still within our suggestion range).";

    public string ScheduleExtendedAfter(DateTime departure, int days) => _vi
        ? $"Khởi hành {departure:dd/MM/yyyy} — muộn hơn {days} ngày (xa hơn một chút so với khoảng bạn chọn)."
        : $"Departs {departure:yyyy-MM-dd} — {days} day(s) later (a bit beyond your preferred window).";

    public string ReasonWishlistHistory => T(
        "Based on wishlist/booking history and personal preferences.",
        "Dựa trên lịch sử wishlist/đặt tour và sở thích cá nhân.");

    public string ReasonPopularTour => T(
        "Popular tour with strong ratings in the current catalog.",
        "Tour phổ biến, đánh giá cao trong catalog hiện tại.");

    public string ReasonSimilarTour(string tourName) => _vi
        ? $"Tương tự tour \"{tourName}\" (content-based ML)."
        : $"Similar to \"{tourName}\" (content-based ML).";

    public string WeatherForecastSummary(string place, double min, double max, double rain, int days) => _vi
        ? $"Dự báo thời tiết {place}: nhiệt độ {min:0.#}–{max:0.#}°C, mưa tích lũy ~{rain:0.#}mm trong {days} ngày."
        : $"Weather forecast for {place}: {min:0.#}–{max:0.#}°C, ~{rain:0.#}mm rain over {days} day(s).";

    public string WeatherHistoricalSummary(string place, double min, double max, double rain) => _vi
        ? $"Thống kê thời tiết lịch sử (cùng kỳ năm trước) tại {place}: {min:0.#}–{max:0.#}°C, mưa ~{rain:0.#}mm."
        : $"Historical weather (same period last year) for {place}: {min:0.#}–{max:0.#}°C, ~{rain:0.#}mm rain.";

    public string WeatherImpactRainy => T(
        "Rainy period — favor indoor tours, early-morning floating markets, rain gear, and flexible schedules.",
        "Thời điểm này thường mưa nhiều — ưu tiên tour trong nhà, chợ nổi buổi sáng sớm, mang áo mưa và ưu tiên lịch trình linh hoạt.");

    public string WeatherImpactHot => T(
        "Hot weather — choose early departures, midday breaks, hydration, and beach/evening activities.",
        "Nhiệt độ cao — chọn tour có giờ khởi hành sớm, nghỉ trưa hợp lý, bổ sung nước và ưu tiên biển/đêm.");

    public string WeatherImpactCool => T(
        "Cool weather — good for light trekking and culture tours; pack a light jacket.",
        "Thời tiết mát — phù hợp trekking nhẹ, tham quan văn hóa; nên mang thêm áo khoác mỏng.");

    public string WeatherImpactMild => T(
        "Stable weather — suitable for most outdoor tours, beaches, and cultural sites.",
        "Thời tiết ổn định — phù hợp hầu hết tour ngoài trời, tắm biển và tham quan điểm văn hóa.");

    private string FormatInterestLabel(string key) => key.ToLowerInvariant() switch
    {
        "beach" => OptionBeach,
        "culture" => OptionCulture,
        "nature" => OptionNature,
        "food" => OptionFood,
        "adventure" => OptionAdventure,
        "relax" => OptionRelax,
        "photography" => OptionPhotography,
        "city" => OptionCity,
        "river" => OptionRiver,
        _ => key
    };

    private string T(string en, string vi) => _vi ? vi : en;
}
