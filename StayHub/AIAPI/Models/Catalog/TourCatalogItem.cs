namespace AIAPI.Models.Catalog;

public class TourCatalogItem
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? ImageUrl { get; set; }
    public string Status { get; set; } = "Active";
    public double? AverageStar { get; set; }
    public int ReviewCount { get; set; }
    public long? MinPrice { get; set; }
    public long? MaxPrice { get; set; }
    public DateTime? NextDeparture { get; set; }
    public int? DurationDays { get; set; }
    public string SearchDocument { get; set; } = "";
    public List<string> ItineraryTitles { get; set; } = new();
    public List<int> TourismInfoIds { get; set; } = new();
}
