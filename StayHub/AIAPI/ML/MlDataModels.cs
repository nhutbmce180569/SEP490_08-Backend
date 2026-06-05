using Microsoft.ML.Data;

namespace AIAPI.ML;

public class IntentExample
{
    public string Text { get; set; } = "";
    public string Label { get; set; } = "";
}

public class IntentPrediction
{
    [ColumnName("PredictedLabel")]
    public string Label { get; set; } = "";

    public float[] Score { get; set; } = Array.Empty<float>();
}

public class TourDocument
{
    public int TourId { get; set; }
    public string Text { get; set; } = "";
}

public class TourismDocument
{
    public int TourismId { get; set; }
    public string Text { get; set; } = "";
}

public class ScoredTourDocument
{
    public int TourId { get; set; }
    public float Score { get; set; }
}

public static class TourIntents
{
    public const string SearchTour = "search_tour";
    public const string RecommendTour = "recommend_tour";
    public const string AskCulture = "ask_culture";
    public const string AskBudget = "ask_budget";
    public const string AskDestination = "ask_destination";
    public const string Greeting = "greeting";
    public const string AskSystem = "ask_system";
    public const string Unknown = "unknown";
}

public static class IntentTrainingData
{
    public static IReadOnlyList<IntentExample> GetSamples() => new List<IntentExample>
    {
        new() { Label = TourIntents.Greeting, Text = "xin chao" },
        new() { Label = TourIntents.Greeting, Text = "hello" },
        new() { Label = TourIntents.Greeting, Text = "chao ban" },
        new() { Label = TourIntents.Greeting, Text = "hi stayhub" },

        new() { Label = TourIntents.SearchTour, Text = "tim tour di da lat" },
        new() { Label = TourIntents.SearchTour, Text = "co tour bien nao khong" },
        new() { Label = TourIntents.SearchTour, Text = "search beach tour vietnam" },
        new() { Label = TourIntents.SearchTour, Text = "toi muon tim tour phu quoc 3 ngay" },
        new() { Label = TourIntents.SearchTour, Text = "tour trekking mien tay" },

        new() { Label = TourIntents.RecommendTour, Text = "goi y tour cho toi" },
        new() { Label = TourIntents.RecommendTour, Text = "ban co tour nao phu hop khong" },
        new() { Label = TourIntents.RecommendTour, Text = "recommend tour for me" },
        new() { Label = TourIntents.RecommendTour, Text = "tour phu hop voi toi la gi" },
        new() { Label = TourIntents.RecommendTour, Text = "de xuat tour ca nhan hoa" },

        new() { Label = TourIntents.AskBudget, Text = "tour duoi 5 trieu" },
        new() { Label = TourIntents.AskBudget, Text = "co tour gia re khong" },
        new() { Label = TourIntents.AskBudget, Text = "budget under 3 million vnd" },
        new() { Label = TourIntents.AskBudget, Text = "ngan sach 2 trieu 1 nguoi" },

        new() { Label = TourIntents.AskDestination, Text = "di da nang thang 7 co tour nao" },
        new() { Label = TourIntents.AskDestination, Text = "diem den nao phu hop mua he" },
        new() { Label = TourIntents.AskDestination, Text = "best destination in vietnam" },
        new() { Label = TourIntents.AskDestination, Text = "noi nao dep de di du lich" },

        new() { Label = TourIntents.AskCulture, Text = "dac san da lat la gi" },
        new() { Label = TourIntents.AskCulture, Text = "van hoa dia phuong o hue" },
        new() { Label = TourIntents.AskCulture, Text = "local food in hoi an" },
        new() { Label = TourIntents.AskCulture, Text = "cho toi biet ve di tich o tour nay" },
        new() { Label = TourIntents.AskCulture, Text = "thong tin du lich chinh thong ve sapa" },

        new() { Label = TourIntents.AskSystem, Text = "stayhub la gi" },
        new() { Label = TourIntents.AskSystem, Text = "lam sao dat tour" },
        new() { Label = TourIntents.AskSystem, Text = "cach thanh toan tren stayhub" },
        new() { Label = TourIntents.AskSystem, Text = "voucher dung the nao" },
        new() { Label = TourIntents.AskSystem, Text = "ho tro khach hang o dau" },
        new() { Label = TourIntents.AskSystem, Text = "tro ly ai co the lam gi" },
        new() { Label = TourIntents.AskSystem, Text = "how to book a tour on stayhub" },
        new() { Label = TourIntents.AskSystem, Text = "what payment methods are supported" },
        new() { Label = TourIntents.AskSystem, Text = "how does the ai questionnaire work" },
        new() { Label = TourIntents.AskSystem, Text = "he thong stayhub gom nhung gi" },
        new() { Label = TourIntents.AskSystem, Text = "lam sao huy tour va hoan tien" },
        new() { Label = TourIntents.AskSystem, Text = "tinh nang social cua stayhub" }
    };
}
