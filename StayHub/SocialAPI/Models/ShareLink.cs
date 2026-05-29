using System;
using System.Collections.Generic;

namespace SocialAPI.Models;

public partial class ShareLink
{
    public int Id { get; set; }

    public int TicketId { get; set; }

    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }
}
