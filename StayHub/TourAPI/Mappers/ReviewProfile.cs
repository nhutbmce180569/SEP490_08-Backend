using AutoMapper;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Mappers
{
    public class ReviewProfile : Profile
    {
       public ReviewProfile() {
            CreateMap<Review, ReadReviewDTO>()
                .ForMember(dest => dest.Replies, opt => opt.MapFrom(src => src.ReviewReplies));
            CreateMap<CreateReviewDTO, Review>();
            CreateMap<UpdateReviewDTO, Review>();
        }
    }
}
