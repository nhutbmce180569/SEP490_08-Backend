using System.ComponentModel.DataAnnotations;

namespace SocialAPI.DTOs;

public class FriendRequestUpdateDto
{
    [Required(ErrorMessage = "Request ID is required.")]
    public required int RequestId { get; set; }

    [Required(ErrorMessage = "Status is required.")]
    [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
    public required string Status { get; set; }
}