using Microsoft.AspNetCore.Http;

namespace SocialAPI.DTOs;

public class MomentCreateDto
{
    public IFormFile Image { get; set; } = null!;
    public int ScheduleId { get; set; }
    public int UserId { get; set; }
    public string? Caption { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Privacy { get; set; } = "Public";
}