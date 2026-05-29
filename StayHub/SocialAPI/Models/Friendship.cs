using System;
using System.Collections.Generic;

namespace SocialAPI.Models;

public partial class Friendship
{
    public int Id { get; set; }

    public int RequesterId { get; set; }

    public int ReceiverId { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }
}
