namespace SocialAPI.DTOs;

public class ReportRequestDto
{
    public string ContentType { get; set; } = null!;
    
    public int TargetId { get; set; }
    
    public string Reason { get; set; } = null!;
    
    public string? Details { get; set; }
}
