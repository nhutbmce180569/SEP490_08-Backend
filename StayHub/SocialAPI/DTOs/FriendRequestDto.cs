using System.ComponentModel.DataAnnotations;

namespace SocialAPI.DTOs;

public class FriendRequestDto
{
    [Required(ErrorMessage = "Receiver ID is required.")]
    public required int ReceiverId { get; set; }
}