    using Microsoft.EntityFrameworkCore;
using SocialAPI.DTOs;
using SocialAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SocialAPI.Repositories.Implements;

public class MomentRepository : IMomentRepository
{
    private readonly StayHubSocialDbContext _context;

    public MomentRepository(StayHubSocialDbContext context)
    {
        _context = context;
    }

    public async Task<TourMoment> CreateMomentAsync(TourMoment moment)
    {
        await _context.TourMoments.AddAsync(moment);
        await _context.SaveChangesAsync();
        return moment;
    }

    // Đã sửa int thành int? để khớp với Interface
    public async Task<IEnumerable<TourMoment>> GetMomentsByScheduleIdAsync(int? scheduleId, int currentUserId)
    {
        var query = _context.TourMoments
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(m => m.MomentComments.Where(c => c.Status == "Approved"))
            .Include(m => m.MomentReactions)
            .Where(m => m.Status == "Approved")
            .AsQueryable();

        if (scheduleId.HasValue && scheduleId.Value > 0)
        {
            query = query.Where(m => m.ScheduleId == scheduleId.Value);
        }

        query = query.Where(m => m.UserId == currentUserId ||
                     m.Privacy == "Public" ||
                     m.Privacy == "Tour" ||
                     (m.Privacy == "Friend" && _context.Friendships.Any(f => f.Status == "Accepted" &&
                         ((f.RequesterId == currentUserId && f.ReceiverId == m.UserId) ||
                          (f.ReceiverId == currentUserId && f.RequesterId == m.UserId)))));

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .ToListAsync();
    }

    public async Task<IEnumerable<TourMoment>> GetMomentFeedPagedAsync(int? scheduleId, int currentUserId, int skip, int top)
    {
        var query = _context.TourMoments
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(m => m.MomentComments.Where(c => c.Status == "Approved"))
            .Include(m => m.MomentReactions)
            .Where(m => m.Status == "Approved")
            .AsQueryable();

        if (scheduleId.HasValue && scheduleId.Value > 0)
        {
            query = query.Where(m => m.ScheduleId == scheduleId.Value);
        }

        query = query.Where(m =>
            m.UserId == currentUserId ||
            m.Privacy == "Public" ||
            m.Privacy == "Tour" ||
            (m.Privacy == "Friend" && _context.Friendships.Any(f => f.Status == "Accepted" &&
                ((f.RequesterId == currentUserId && f.ReceiverId == m.UserId) ||
                 (f.ReceiverId == currentUserId && f.RequesterId == m.UserId)))));

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip(skip)
            .Take(top)
            .ToListAsync();
    }
    public IQueryable<TourMoment> GetMomentsAsQueryable()
    {
        return _context.TourMoments.Where(m => m.Status == "Approved").AsQueryable();
    }

    public async Task<TourMoment?> GetMomentByIdAsync(int id)
    {
        return await _context.TourMoments
            .Include(m => m.MomentComments.Where(c => c.Status == "Approved"))
            .Include(m => m.MomentReactions)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task DeleteMomentAsync(TourMoment moment)
    {
        if (moment.MomentReactions.Any()) _context.MomentReactions.RemoveRange(moment.MomentReactions);
        if (moment.MomentComments.Any()) _context.MomentComments.RemoveRange(moment.MomentComments);

        _context.TourMoments.Remove(moment);
        await _context.SaveChangesAsync();
    }

    public async Task<MomentReaction?> GetReactionAsync(int momentId, int userId) =>
        await _context.MomentReactions.FirstOrDefaultAsync(r => r.MomentId == momentId && r.UserId == userId);

    public async Task AddReactionAsync(MomentReaction reaction)
    {
        await _context.MomentReactions.AddAsync(reaction);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveReactionAsync(MomentReaction reaction)
    {
        _context.MomentReactions.Remove(reaction);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateReactionAsync(MomentReaction reaction)
    {
        _context.MomentReactions.Update(reaction);
        await _context.SaveChangesAsync();
    }

    public async Task<MomentComment> AddCommentAsync(MomentComment comment)
    {
        await _context.MomentComments.AddAsync(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    public async Task<MomentComment?> GetCommentByIdAsync(int commentId) =>
        await _context.MomentComments.FirstOrDefaultAsync(c => c.Id == commentId);

    public async Task UpdateCommentAsync(MomentComment comment) { _context.MomentComments.Update(comment); await _context.SaveChangesAsync(); }
    public async Task DeleteCommentAsync(MomentComment comment) { _context.MomentComments.Remove(comment); await _context.SaveChangesAsync(); }

    public async Task<List<FootprintDto>> GetUserFootprintsAsync(int userId)
    {
        return await _context.TourMoments
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Lat.HasValue && m.Lng.HasValue && m.Status == "Approved")
            // Project to DTO, rounding coordinates to 3 decimal places for grouping nearby points.
            .Select(m => new FootprintDto
            {
                Lat = Math.Round(m.Lat.Value, 3),
                Lng = Math.Round(m.Lng.Value, 3)
            })
            // The database will return only unique coordinate pairs.
            .Distinct()
            .ToListAsync();
    }

    public async Task<IEnumerable<TourMoment>> GetUserMomentsAsync(int targetUserId, int currentUserId, bool isFriend)
    {
        var query = _context.TourMoments
            .AsNoTracking()
            .Where(m => m.UserId == targetUserId && m.Status == "Approved");

        if (currentUserId != targetUserId)
        {
            if (isFriend)
            {
                query = query.Where(m => m.Privacy == "Public" || m.Privacy == "Friend");
            }
            else
            {
                query = query.Where(m => m.Privacy == "Public");
            }
        }

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
    }
}