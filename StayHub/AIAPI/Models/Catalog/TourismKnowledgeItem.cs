namespace AIAPI.Models.Catalog;

public class TourismKnowledgeItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Description { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public string SearchDocument { get; set; } = "";
}
