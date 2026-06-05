namespace AIAPI.Models.Knowledge;

public class CulturalKnowledgeEntry
{
    public List<string> CityKeys { get; set; } = new();
    public string DisplayName { get; set; } = "";
    public string? DisplayNameVi { get; set; }
    public string Region { get; set; } = "";
    public List<string> Facts { get; set; } = new();
    public List<string>? FactsVi { get; set; }
    public List<string> ForeignVisitorNotes { get; set; } = new();
    public List<string>? ForeignVisitorNotesVi { get; set; }
    public List<string> ElderlyNotes { get; set; } = new();
    public List<string>? ElderlyNotesVi { get; set; }
    public List<string> ChildNotes { get; set; } = new();
    public List<string>? ChildNotesVi { get; set; }
    public List<CulturalSourceRef> Sources { get; set; } = new();
}

public class CulturalSourceRef
{
    public string Name { get; set; } = "";
    public string Url { get; set; } = "";
    public string Authority { get; set; } = "";
}

public class CulturalKnowledgeBundle
{
    public string Version { get; set; } = "";
    public string? Description { get; set; }
    public List<CulturalKnowledgeEntry> Entries { get; set; } = new();
}

public class RagCorpusChunk
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public List<string> CityKeys { get; set; } = new();
    public string Region { get; set; } = "";
    public List<string> InterestTags { get; set; } = new();
    public List<string> PersonaTags { get; set; } = new();
    public List<int> RelatedTourIds { get; set; } = new();
    public string HeritageLevel { get; set; } = "";
    public string ChunkType { get; set; } = "";
    public CulturalSourceRef Source { get; set; } = new();

    public string BuildSearchDocument() =>
        $"{Title}. {Content} Tags: {string.Join(", ", InterestTags)} {string.Join(", ", PersonaTags)} {Region}";
}

public class RagCorpusBundle
{
    public string Version { get; set; } = "";
    public string? Description { get; set; }
    public RagPaperMetadata? PaperMetadata { get; set; }
    public RagCorpusStatistics? Statistics { get; set; }
    public List<RagCorpusChunk> Chunks { get; set; } = new();
}

public class RagPaperMetadata
{
    public string CorpusName { get; set; } = "";
    public string TargetVenue { get; set; } = "";
    public List<string> SourceTypes { get; set; } = new();
    public string ChunkSchema { get; set; } = "";
}

public class RagCorpusStatistics
{
    public int TotalChunks { get; set; }
    public int UnescoChunks { get; set; }
    public List<string> ChunkTypes { get; set; } = new();
    public List<string> InterestCoverage { get; set; } = new();
    public List<string> PersonaCoverage { get; set; } = new();
}

public class RagRetrievalResult
{
    public RagCorpusChunk Chunk { get; set; } = new();
    public float Score { get; set; }
}

public class RagCorpusStatsDTO
{
    public string CorpusVersion { get; set; } = "";
    public string CorpusName { get; set; } = "";
    public int TotalChunks { get; set; }
    public int EmbeddedCorpusEntries { get; set; }
    public int ContentApiTourismItems { get; set; }
    public int UnescoChunks { get; set; }
    public List<string> ChunkTypes { get; set; } = new();
    public List<string> InterestCoverage { get; set; } = new();
    public List<string> PersonaTags { get; set; } = new();
    public List<string> KnowledgeProviders { get; set; } = new();
    public string PaperCitationHint { get; set; } = "";
}

public class CulturalFactResult
{
    public string Fact { get; set; } = "";
    public string SourceName { get; set; } = "";
    public string SourceUrl { get; set; } = "";
    public string AuthorityLevel { get; set; } = "";
    public string Provider { get; set; } = "";
    public string? City { get; set; }
}
