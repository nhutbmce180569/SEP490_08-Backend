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
            .ForMember(dest => dest.TotalLikes,
                opt => opt.MapFrom(src => src.MomentReactions.Count(r => r.IsLike == true)))
            // ✅ Map ReactionCount để JSON có key "reactionCount" cho Flutter
            .ForMember(dest => dest.ReactionCount,
                opt => opt.MapFrom(src => src.MomentReactions.Count(r => r.IsLike == true)))
            // IsLikedByMe không map ở đây (cần currentUserId) -> set trong Service
            .ForMember(dest => dest.IsLikedByMe, opt => opt.Ignore())
            // Giữ lại MomentUserDto đã được khởi tạo, tránh bị AutoMapper set thành null
            .ForMember(dest => dest.User, opt => opt.Ignore())
            .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.MomentComments))
            .ForMember(dest => dest.Privacy, opt => opt.MapFrom(src => src.Privacy ?? "Public"));

        CreateMap<MomentComment, CommentResponseDto>();
        CreateMap<MomentReaction, ReactionRequestDto>();

        CreateMap<FriendRequestDto, Friendship>();

        CreateMap<Friendship, FriendshipResponseDto>()
            .ForMember(dest => dest.FriendId, opt => opt.MapFrom((src, dest, destMember, context) =>
            {
                if (context.TryGetItems(out var items) &&
                    items.TryGetValue("CurrentUserId", out var currentUserIdObj) &&
                    currentUserIdObj is int currentUserId)
                {
                    return src.RequesterId == currentUserId ? src.ReceiverId : src.RequesterId;
                }
                return 0;
            }))
            .ForMember(dest => dest.FullName, opt => opt.Ignore());

        CreateMap<TourMoment, UserMomentResponseDto>();
    }
}
