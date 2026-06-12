using System;

namespace SocialAPI.DTOs;

public class FootprintDto
{
    public double Lat { get; set; }
    public double Lng { get; set; }
    public DateTime? Timestamp { get; set; }
}