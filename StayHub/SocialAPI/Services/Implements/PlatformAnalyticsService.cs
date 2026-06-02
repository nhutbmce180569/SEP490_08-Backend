using Microsoft.EntityFrameworkCore;
using SocialAPI.DTOs;
using SocialAPI.Models;

namespace SocialAPI.Services.Implements;

public class PlatformAnalyticsService : IPlatformAnalyticsService
{
    private readonly StayHubSocialDbContext _context;

    public PlatformAnalyticsService(StayHubSocialDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformSocialStatsDTO> GetSocialStatsAsync()
    {
        var friendships = await _context.Friendships.AsNoTracking().ToListAsync();
        var chatRooms = await _context.ChatRooms.AsNoTracking().ToListAsync();
        var messages = await _context.ChatMessages.AsNoTracking().ToListAsync();
        var moments = await _context.TourMoments.AsNoTracking().ToListAsync();
        var reactions = await _context.MomentReactions.AsNoTracking().CountAsync();
        var comments = await _context.MomentComments.AsNoTracking().CountAsync();
        var locationLogs = await _context.LocationLogs.AsNoTracking().CountAsync();

        var fsTotal = friendships.Count;

        return new PlatformSocialStatsDTO
        {
            TotalFriendships = fsTotal,
            AcceptedFriendships = friendships.Count(f => f.Status == "Accepted"),
            PendingFriendRequests = friendships.Count(f => f.Status == "Pending"),
            DeclinedFriendRequests = friendships.Count(f => f.Status == "Declined"),
            TotalChatRooms = chatRooms.Count,
            GroupChatRooms = chatRooms.Count(r => r.IsGroupChat == true),
            PrivateChatRooms = chatRooms.Count(r => r.IsGroupChat != true),
            TotalChatMessages = messages.Count,
            UnreadChatMessages = messages.Count(m => m.IsRead != true),
            TotalTourMoments = moments.Count,
            TotalMomentReactions = reactions,
            TotalMomentComments = comments,
            TotalLocationLogs = locationLogs,
            MomentsByPrivacy = moments.Count > 0
                ? moments
                    .GroupBy(m => m.Privacy ?? "Public")
                    .Select(g => new LabelCountDTO
                    {
                        Label = g.Key,
                        Count = g.Count(),
                        Percentage = Math.Round(g.Count() * 100m / moments.Count, 2)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList()
                : [],
            FriendshipStatusDistribution = fsTotal > 0
                ? friendships
                    .GroupBy(f => f.Status ?? "Unknown")
                    .Select(g => new LabelCountDTO
                    {
                        Label = g.Key,
                        Count = g.Count(),
                        Percentage = Math.Round(g.Count() * 100m / fsTotal, 2)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList()
                : []
        };
    }
}
