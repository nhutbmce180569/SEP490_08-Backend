using System.Collections.Generic;

namespace SocialAPI.DTOs;

public class PaginationDTO<T>
{
    public IEnumerable<T> Data { get; set; } = new List<T>();
    public int Total { get; set; }
}