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
    string QuestionAdultCount { get; }
    string QuestionChildrenCount { get; }
    string QuestionElderlyCount { get; }
    string QuestionTravelPace { get; }
    string OptionPaceRelaxed { get; }
    string OptionPaceModerate { get; }
    string OptionPacePacked { get; }
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
    string SummaryFoundRelaxed(int personaCount, int tourCount, string topScore, string? weatherNote);
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
    string ChatToursFoundForCity(int count, string city);
    string ChatNoTours { get; }
    string ChatNoToursForCity(string city);
    string ChatRecommendFound(int count);
    string ChatRecommendNone { get; }
    string ChatCultureNoData { get; }
    string ChatCultureNoDataForCity(string city);
    string ChatCultureReply(string source, string name, string type, string description);
    string ChatSystemReply(string title, string content);
    string ChatSystemNoData { get; }
    string ChatSystemMultiHeader { get; }
    IReadOnlyList<string> ChatSystemSuggestions { get; }
    string ChatFallbackHelp { get; }
    string ChatRagInsight(string fact);

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
    string ReasonQueryMatch { get; }
    string ReasonCatalogQuality { get; }
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
    public string QuestionStartDate => T("Departure date?", "Ngày khởi hành?");
    public string QuestionStartDateHint => T(
        "For dates ≤14 days, you'll receive accurate weather forecasts.",
        "Đi sớm (≤14 ngày) hệ thống sẽ cung cấp dự báo thời tiết chi tiết.");
    public string QuestionEndDate => T("Return date?", "Ngày kết thúc?");
    public string QuestionBudget => T("Max budget per person (VND)?", "Ngân sách tối đa/người (VND)?");
    public string QuestionHasElderly => T("Any elderly travelers?", "Có người cao tuổi đi cùng không?");
    public string QuestionHasChildren => T("Any children traveling?", "Có trẻ em đi cùng không?");
    public string QuestionAdultCount => T("Number of adults", "Số lượng người lớn");
    public string QuestionChildrenCount => T("Number of children (Under 12 years old)", "Số lượng trẻ em (Dưới 12 tuổi)");
    public string QuestionElderlyCount => T("Number of elderly (Over 60 years old)", "Số lượng người cao tuổi (Trên 60 tuổi)");
    public string QuestionTravelPace => T("Travel pace", "Nhịp độ chuyến đi");
    public string OptionPaceRelaxed => T("Relaxed, gentle", "Thư giãn, nhẹ nhàng");
    public string OptionPaceModerate => T("Balanced (Exploration & leisure)", "Cân bằng (Khám phá và nghỉ dưỡng)");
    public string OptionPacePacked => T("Fast-paced, maximum exploration", "Lịch trình dày, khám phá tối đa");
    public string QuestionInterests => T("What are your travel interests?", "Sở thích du lịch của bạn là gì?");
    public string OptionBeach => T("Beach / Islands", "Biển / Đảo");
    public string OptionCulture => T("Culture & Heritage", "Văn hóa & Di sản");
    public string OptionNature => T("Nature & Mountains", "Thiên nhiên & Đồi núi");
    public string OptionFood => T("Food Tour", "Khám phá ẩm thực");
    public string OptionAdventure => T("Adventure & Trekking", "Mạo hiểm / Trekking");
    public string OptionRelax => T("Healing / Resort", "Nghỉ dưỡng / Resort");
    public string OptionPhotography => T("Sightseeing / Photography", "Tham quan / Chụp ảnh");
    public string OptionCity => T("City Exploration", "Khám phá thành phố");
    public string OptionRiver => T("River / Floating Markets", "Sông nước / Chợ nổi");
    public string QuestionNationality => T("Are you a local or international guest?", "Bạn là khách nội địa hay quốc tế?");
    public string OptionVietnamese => T("Local (Vietnamese)", "Khách nội địa (Việt Nam)");
    public string OptionForeigner => T("International guest", "Khách quốc tế");
    public string QuestionPreferredCity => T("Preferred destination?", "Điểm đến mong muốn?");
    public string QuestionPreferredCityHint => T("Skip if you want us to surprise you!", "Bỏ qua nếu bạn muốn nhận gợi ý ngẫu nhiên!");

    public string PersonaPrimary => T("Primary traveler", "Du khách chính");
    public string PersonaElderly => T("Elderly companion", "Người cao tuổi đi cùng");
    public string PersonaChild => T("Child companion", "Trẻ em đi cùng");
    public string PersonaInternational => T("International guest", "Khách quốc tế");
    public string PersonaCouple => T("Couple vibe", "Không khí couple");
    public string PersonaFamily => T("Family vibe", "Không khí gia đình");
    public string PersonaGroup => T("Group vibe", "Không khí nhóm");

    public string ReasonElderlyGood(string persona, float score) => _vi
        ? "Lịch trình nhẹ nhàng, phù hợp cho người cao tuổi."
        : "A relaxed pace that works perfectly for elderly travelers.";

    public string ReasonElderlyPoor(string persona) => _vi
        ? "Lịch trình có nhiều hoạt động thể lực, cần cân nhắc cho người cao tuổi."
        : "May be tiring for elderly travelers (lots of walking or strenuous activities).";

    public string ReasonChildGood(string persona, float score) => _vi
        ? "Có nhiều hoạt động giải trí phù hợp cho trẻ em."
        : "Includes activities that kids will absolutely love.";

    public string ReasonChildPoor(string persona) => _vi
        ? "Lịch trình khá dài và nhiều di chuyển, cần cân nhắc khi có trẻ nhỏ."
        : "Worth a closer look if traveling with young kids (long days or adventure-heavy).";

    public string ReasonInternationalGood(string persona) => _vi
        ? "Dễ dàng tham gia và tìm hiểu văn hóa bản địa."
        : "Easy to follow and explore local culture — good for international visitors.";

    public string ReasonInterestMatch(string persona, IEnumerable<string> interests)
    {
        return _vi
            ? "Phù hợp với sở thích du lịch bạn đã chọn."
            : "Matches your selected travel vibe.";
    }

    public string ReasonLocationMatch(string city) => _vi
        ? $"Nằm trong khu vực bạn mong muốn: {city}."
        : $"In the area you asked for: {city}.";

    public string ReasonBudgetFit => _vi
        ? "Mức giá phù hợp với ngân sách của bạn."
        : "Priced perfectly within the budget you shared.";

    public string ReasonInterestStrong => _vi
        ? "Đặc điểm tour rất sát với mong muốn của bạn."
        : "Tour vibe matches perfectly with what you love.";

    public string ReasonScheduleFit => _vi
        ? "Có lịch khởi hành phù hợp với thời gian bạn chọn."
        : "Has a departure date that fits exactly in your travel window.";

    public string ReasonGoodRating => _vi
        ? "Được đánh giá cao bởi các du khách trước đó."
        : "Highly rated by travelers who joined before.";

    public string SummaryNoTours => T(
        "No tours satisfy the hard constraints. Try increasing your budget or changing the destination.",
        "Không có tour thỏa ràng buộc cứng. Thử nới ngân sách hoặc đổi điểm đến.");

    public string SummaryFound(int personaCount, int tourCount, string topScore, string? weatherNote) => _vi
        ? $"Dựa trên sở thích và người đi cùng bạn, chúng tôi tìm thấy {tourCount} tour phù hợp — tour khớp nhất đạt {topScore}.{weatherNote ?? ""}"
        : $"Based on your preferences and travel party, we found {tourCount} matching tour(s) — best fit {topScore}.{weatherNote ?? ""}";

    public string SummaryFoundRelaxed(int personaCount, int tourCount, string topScore, string? weatherNote) => _vi
        ? $"Chúng tôi không tìm thấy tour khớp 100% yêu cầu cứng. Nhưng đừng lo, AI đã tự động nới lỏng ngân sách/địa điểm và tìm ra {tourCount} tour xuất sắc thay thế — tour khớp nhất đạt {topScore}.{weatherNote ?? ""}"
        : $"We couldn't find exact matches for your strict constraints. However, we automatically relaxed the budget/city and found {tourCount} excellent alternatives — best fit {topScore}.{weatherNote ?? ""}";

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
        "Hello! I'm StayHub's AI assistant. I can help with tour search, personalized recommendations, destination culture, and platform questions (booking, payment, vouchers, AI features).",
        "Xin chào! Tôi là trợ lý AI của StayHub. Tôi có thể giúp tìm tour, gợi ý cá nhân hóa, văn hóa điểm đến và giải đáp về hệ thống (đặt tour, thanh toán, voucher, tính năng AI).");

    public IReadOnlyList<string> ChatDefaultSuggestions => _vi
        ?
        [
            "Gợi ý tour biển giá dưới 5 triệu",
            "Tour Đà Lạt 3 ngày tháng 7",
            "StayHub là gì và có những tính năng gì?",
            "Làm sao đặt tour và thanh toán?"
        ]
        :
        [
            "Beach tours under 5M VND",
            "3-day Da Lat tour in July",
            "What is StayHub and what can it do?",
            "How do I book and pay for a tour?"
        ];

    public string ChatCultureSuggest1 => T("Suggest tours for this destination", "Gợi ý tour phù hợp tại đây");
    public string ChatCultureSuggest2 => T("Budget-friendly tours here", "Tour giá rẻ cho điểm đến này");
    public string ChatCultureSuggest3 => T("Sample 3-day itinerary", "Lịch trình mẫu 3 ngày");

    public string ChatToursFound(int count) => _vi
        ? $"Tôi tìm thấy {count} tour phù hợp với yêu cầu của bạn."
        : $"I found {count} tours matching your request.";

    public string ChatToursFoundForCity(int count, string city) => _vi
        ? $"Tôi tìm thấy {count} tour tại {city} phù hợp yêu cầu của bạn."
        : $"I found {count} tour(s) in {city} matching your request.";

    public string ChatNoTours => T(
        "No tour matched perfectly. Try relaxing your budget or changing the city/dates.",
        "Chưa có tour khớp hoàn toàn. Bạn thử nới ngân sách hoặc đổi thành phố/thời gian khác nhé.");

    public string ChatNoToursForCity(string city) => _vi
        ? $"Hiện chưa có tour nào tại {city} khớp yêu cầu. Bạn thử đổi ngân sách, số ngày hoặc hỏi gợi ý tour khác."
        : $"No tours in {city} match your request yet. Try adjusting budget, duration, or ask for other suggestions.";

    public string ChatRecommendFound(int count) => _vi
        ? $"Dựa trên yêu cầu của bạn, tôi gợi ý {count} tour phù hợp."
        : $"Based on your request, here are {count} matching tour suggestion(s).";

    public string ChatRecommendNone => T(
        "I could not find tours matching your request. Try adjusting city, budget, or travel dates.",
        "Chưa tìm thấy tour khớp yêu cầu. Thử đổi thành phố, ngân sách hoặc ngày đi.");

    public string ChatCultureNoData => T(
        "I don't have authoritative reference data for that question. Try asking about a specific city.",
        "Tôi chưa có dữ liệu tham khảo chính thống khớp câu hỏi. Bạn thử hỏi theo tên thành phố cụ thể.");

    public string ChatCultureNoDataForCity(string city) => _vi
        ? $"Chưa có bài viết tham khảo active cho {city}. Bạn có thể xem các tour liên quan bên dưới."
        : $"No active reference articles for {city}. See related tours below.";

    public string ChatCultureReply(string source, string name, string type, string description) => _vi
        ? $"Theo {source}, {name} ({type}): {description}"
        : $"According to {source}, {name} ({type}): {description}";

    public string ChatSystemReply(string title, string content) => _vi
        ? $"{title}\n{content}"
        : $"{title}\n{content}";

    public string ChatSystemNoData => T(
        "I don't have a specific answer for that yet. Try asking about booking, payments, vouchers, the AI assistant, or tour search.",
        "Tôi chưa có câu trả lời cụ thể cho câu hỏi này. Bạn thử hỏi về đặt tour, thanh toán, voucher, trợ lý AI hoặc tìm tour nhé.");

    public string ChatSystemMultiHeader => T(
        "Here's what I know about StayHub:",
        "Đây là thông tin về hệ thống StayHub:");

    public IReadOnlyList<string> ChatSystemSuggestions => _vi
        ?
        [
            "StayHub là gì và có những tính năng gì?",
            "Làm sao đặt tour trên StayHub?",
            "Thanh toán trên StayHub hoạt động thế nào?",
            "Trợ lý AI StayHub có thể làm gì?"
        ]
        :
        [
            "What is StayHub and what can it do?",
            "How do I book a tour on StayHub?",
            "How does payment work on StayHub?",
            "What can the StayHub AI assistant do?"
        ];

    public string ChatFallbackHelp => T(
        "I can help with tour search, personalized recommendations, destination culture, and StayHub platform questions (booking, payment, vouchers, AI features). What would you like to know?",
        "Tôi có thể giúp tìm tour, gợi ý cá nhân hóa, văn hóa điểm đến và giải đáp về hệ thống StayHub (đặt tour, thanh toán, voucher, tính năng AI). Bạn muốn hỏi gì?");

    public string ChatRagInsight(string fact) => _vi
        ? $"📚 {fact}"
        : $"📚 {fact}";

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

    public string ReasonQueryMatch => T(
        "Matches your stated preferences (semantic content-based retrieval).",
        "Khớp sở thích/điều kiện bạn cung cấp (tìm kiếm ngữ nghĩa theo nội dung tour).");

    public string ReasonCatalogQuality => T(
        "Strong catalog quality signal (rating and review volume).",
        "Tour có chất lượng tốt trong catalog (đánh giá và số review).");

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
