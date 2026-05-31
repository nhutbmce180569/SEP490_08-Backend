using AutoMapper;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Mappers
{
    public class WishlistProfile : Profile
    {
        public WishlistProfile()
        {
            CreateMap<Wishlist, ReadWishlistItemDTO>()
                .ForMember(dest => dest.WishlistId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.TourName, opt => opt.MapFrom(src => src.Tour != null ? src.Tour.Name : string.Empty))
                .ForMember(dest => dest.TourImageUrl, opt => opt.MapFrom(src => src.Tour != null ? src.Tour.ImageUrl : null))
                .ForMember(dest => dest.TourStatus, opt => opt.MapFrom(src => src.Tour != null ? src.Tour.Status : null))
                .ForMember(dest => dest.TourDescription, opt => opt.MapFrom(src => src.Tour != null ? src.Tour.Description : null));
        }
    }
}
