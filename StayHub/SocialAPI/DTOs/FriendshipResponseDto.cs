using System;

namespace SocialAPI.DTOs;

public class FriendshipResponseDto
{
    public int Id { get; set; }
    public int FriendId { get; set; }
    public int RequesterId { get; set; }
    public required string FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Email { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
}