namespace AIAPI.Models.Knowledge;

public class SystemKnowledgeEntry
{
    public string Id { get; set; } = "";
    public string Category { get; set; } = "";
    public List<string> Keywords { get; set; } = new();
    public string Title { get; set; } = "";
    public string TitleVi { get; set; } = "";
    public string Content { get; set; } = "";
    public string ContentVi { get; set; } = "";
    public List<string> RelatedQuestions { get; set; } = new();
    public List<string> RelatedQuestionsVi { get; set; } = new();
    public List<string> FaqPrompts { get; set; } = new();
    public List<string> FaqPromptsVi { get; set; } = new();

    public string BuildSearchDocument() =>
        $"{Title} {TitleVi} {Content} {ContentVi} {Category} {string.Join(" ", Keywords)} {string.Join(" ", FaqPrompts)} {string.Join(" ", FaqPromptsVi)}";
}

public class SystemKnowledgeBundle
{
    public string Version { get; set; } = "1.0";
    public string? Description { get; set; }
    public List<SystemKnowledgeEntry> Entries { get; set; } = new();
}

public class SystemKnowledgeHit
{
    public SystemKnowledgeEntry Entry { get; set; } = new();
    public float Score { get; set; }
}
