using AutoMapper;
using SocialAPI.DTOs;
using SocialAPI.Models;
using System.Linq;

namespace SocialAPI.Helper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<TourMoment, MomentResponseDto>()
            .ForMember(dest => dest.TotalLikes, opt => opt.MapFrom(src => src.MomentReactions.Count(r => r.IsLike == true)))
            .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.MomentComments));
        CreateMap<MomentComment, CommentResponseDto>();
        CreateMap<MomentReaction, ReactionRequestDto>();
        CreateMap<MomentComment, CommentResponseDto>();

        // Friendship mappings
        CreateMap<FriendRequestDto, Friendship>();
        
        CreateMap<Friendship, FriendshipResponseDto>()
            .ForMember(dest => dest.FriendId, opt => opt.MapFrom((src, dest, destMember, context) =>
            {
                // SỬ DỤNG TryGetItems ĐỂ KHÔNG BỊ CRASH
                if (context.TryGetItems(out var items) && 
                    items.TryGetValue("CurrentUserId", out var currentUserIdObj) && 
                    currentUserIdObj is int currentUserId)
                {
                    return src.RequesterId == currentUserId ? src.ReceiverId : src.RequesterId;
                }
                return 0;
            }))
            .ForMember(dest => dest.FullName, opt => opt.Ignore()); 
            CreateMap<TourMoment, MomentResponseDto>()
    .ForMember(dest => dest.Privacy, opt => opt.MapFrom(src => src.Privacy ?? "Public"));
    }
}