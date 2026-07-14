using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AIAPI.DTOs;

public class ReviewAnalysisRequestDTO
{
    [Required]
    public string ReviewText { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Star rating must be between 1 and 5")]
    public int StarRating { get; set; }
}
