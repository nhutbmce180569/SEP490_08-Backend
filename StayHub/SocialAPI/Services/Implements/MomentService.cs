using AutoMapper;
using AutoMapper.QueryableExtensions;
using SocialAPI.DTOs;
using SocialAPI.DTOs.External;
using SocialAPI.Models;
using SocialAPI.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SocialAPI.Services.Implements;

public class MomentService : IMomentService
{
    private readonly IMomentRepository _momentRepository;
    private readonly ICloudStorageService _cloudStorageService;
    private readonly IMapper _mapper;

    private readonly IHttpClientFactory _httpClientFactory;

    public MomentService(
        IMomentRepository momentRepository,
        ICloudStorageService cloudStorageService,
        IMapper mapper,
        IHttpClientFactory httpClientFactory)
    {
        _momentRepository = momentRepository;
        _cloudStorageService = cloudStorageService;
        _mapper = mapper;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<MomentResponseDto> CreateMomentAsync(MomentCreateDto dto)
    {
        string imageUrl = await _cloudStorageService.UploadImageAsync(dto.Image, "stayhub/social/moments");

        var moment = new TourMoment
        {
            ScheduleId = dto.ScheduleId,
            UserId = dto.UserId,
            ImageUrl = imageUrl, 
            Caption = dto.Caption,
            Lat = dto.Lat,
            Lng = dto.Lng,
            Privacy = dto.Privacy,
            CreatedAt = DateTime.UtcNow
        };

        var createdMoment = await _momentRepository.CreateMomentAsync(moment);
        return _mapper.Map<MomentResponseDto>(createdMoment);
    }

    public async Task<IEnumerable<MomentResponseDto>> GetMomentsByScheduleIdAsync(int scheduleId, int currentUserId)
    {
        var moments = await _momentRepository.GetMomentsByScheduleIdAsync(scheduleId, currentUserId);
        return _mapper.Map<IEnumerable<MomentResponseDto>>(moments);
    }

    public IQueryable<MomentResponseDto> GetMomentsAsQueryable(int scheduleId)
    {
        return _momentRepository.GetMomentsAsQueryable()
            .Where(m => m.ScheduleId == scheduleId)
            .ProjectTo<MomentResponseDto>(_mapper.ConfigurationProvider);
    }

    public async Task ToggleReactionAsync(int momentId, ReactionRequestDto dto)
    {
        var moment = await _momentRepository.GetMomentByIdAsync(momentId);
        if (moment == null) throw new KeyNotFoundException("Moment not found.");

        var existingReaction = await _momentRepository.GetReactionAsync(momentId, dto.UserId);
        if (existingReaction != null)
        {
            await _momentRepository.RemoveReactionAsync(existingReaction);
        }
        else
        {
            var reaction = new MomentReaction { MomentId = momentId, UserId = dto.UserId, IsLike = dto.IsLike };
            await _momentRepository.AddReactionAsync(reaction);
        }
    }

    public async Task<CommentResponseDto> AddCommentAsync(int momentId, CommentRequestDto dto)
    {
        var moment = await _momentRepository.GetMomentByIdAsync(momentId);
        if (moment == null) throw new KeyNotFoundException("Moment not found.");

        var comment = new MomentComment { MomentId = momentId, UserId = dto.UserId, Comment = dto.Comment, Timestamp = DateTime.UtcNow };
        var createdComment = await _momentRepository.AddCommentAsync(comment);

        return _mapper.Map<CommentResponseDto>(createdComment);
    }

    public async Task<CommentResponseDto> UpdateCommentAsync(int commentId, CommentRequestDto dto)
    {
        var comment = await _momentRepository.GetCommentByIdAsync(commentId);
        if (comment == null) throw new KeyNotFoundException("Comment not found.");
        if (comment.UserId != dto.UserId) throw new UnauthorizedAccessException("You do not have permission to update this comment.");

        comment.Comment = dto.Comment;
        await _momentRepository.UpdateCommentAsync(comment);

        return _mapper.Map<CommentResponseDto>(comment);
    }

    public async Task DeleteCommentAsync(int commentId, int userId)
    {
        var comment = await _momentRepository.GetCommentByIdAsync(commentId);
        if (comment == null) throw new KeyNotFoundException("Comment not found.");

        if (comment.UserId != userId) throw new UnauthorizedAccessException("You do not have the right to delete this comment.");

        await _momentRepository.DeleteCommentAsync(comment);
    }

    public async Task DeleteMomentAsync(int momentId, int userId)
    {
        var moment = await _momentRepository.GetMomentByIdAsync(momentId);
        if (moment == null) throw new KeyNotFoundException("Moment not found.");

        if (moment.UserId != userId) throw new UnauthorizedAccessException("You do not have the right to delete this comment.");

        await _momentRepository.DeleteMomentAsync(moment);
    }

    public async Task<IEnumerable<MomentResponseDto>> GetMomentFeedWithUsersAsync(int scheduleId, int currentUserId, int skip, int top)
    {
        var moments = await _momentRepository.GetMomentFeedPagedAsync(scheduleId, currentUserId, skip, top);
        var dtos = _mapper.Map<List<MomentResponseDto>>(moments);

        if (!dtos.Any()) return dtos;

        // Batch fetch users to prevent N+1 HTTP calls
        var userIds = dtos.Select(d => d.UserId).Distinct().ToList();
        var userProfiles = new Dictionary<int, UserProfileShortDto>();

        using var client = _httpClientFactory.CreateClient();
        try
        {
            var response = await client.PostAsJsonAsync("https://localhost:7010/api/users/batch", userIds);
            if (response.IsSuccessStatusCode)
            {
                var apiResult = await response.Content.ReadFromJsonAsync<ApiResponse<List<UserProfileShortDto>>>();
                if (apiResult?.Data != null)
                {
                    userProfiles = apiResult.Data.ToDictionary(u => u.Id, u => u);
                }
            }
        }
        catch 
        { 
            // Intentionally swallowed to allow the feed to load even if the user service is unavailable
        }

        foreach (var dto in dtos)
        {
            if (dto.User == null) continue;

            if (userProfiles.TryGetValue(dto.UserId, out var profile))
            {
                dto.User.FullName = profile.FullName ?? "Anonymous user";
                dto.User.AvatarUrl = profile.AvatarUrl;
            }
            else
            {
                dto.User.FullName = "Anonymous user";
            }
        }

        return dtos;
    }

    public async Task<IEnumerable<FootprintDto>> GetMyFootprintsAsync(int userId)
    {
        return await _momentRepository.GetUserFootprintsAsync(userId);
    }
}