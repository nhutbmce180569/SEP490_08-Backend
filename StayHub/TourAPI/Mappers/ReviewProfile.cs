using AutoMapper;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Mappers
{
    public class ReviewProfile : Profile
    {
       public ReviewProfile() {
            CreateMap<Review, ReadReviewDTO>()
                .ForMember(dest => dest.Replies, opt => opt.MapFrom(src => src.ReviewReplies))
                .ForMember(dest => dest.TourName, opt => opt.MapFrom(src => src.Tour != null ? src.Tour.Name : null));
            CreateMap<CreateReviewDTO, Review>();
            CreateMap<UpdateReviewDTO, Review>()
                .ForMember(dest => dest.CustomerId, opt => opt.Ignore())
                .ForMember(dest => dest.TourId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ReviewReplies, opt => opt.Ignore());
        }
    }
}
