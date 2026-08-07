using AutoMapper;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Mappers
{
    public class TourProfile : Profile
    {
        public TourProfile()
        {
            CreateMap<Tour, ReadTourDTO>()
            .ForMember(dest => dest.AverageStar, opt => opt.MapFrom(src => src.Reviews.Any() ? src.Reviews.Average(x => x.Rating) : 0))
            .ForMember(dest => dest.TotalReviews, opt => opt.MapFrom(src => src.Reviews.Count))
            .ForMember(dest => dest.FiveStarCount, opt => opt.MapFrom(src => src.Reviews.Count(r => r.Rating == 5)))
            .ForMember(dest => dest.FourStarCount, opt => opt.MapFrom(src => src.Reviews.Count(r => r.Rating == 4)))
            .ForMember(dest => dest.ThreeStarCount, opt => opt.MapFrom(src => src.Reviews.Count(r => r.Rating == 3)))
            .ForMember(dest => dest.TwoStarCount, opt => opt.MapFrom(src => src.Reviews.Count(r => r.Rating == 2)))
            .ForMember(dest => dest.OneStarCount, opt => opt.MapFrom(src => src.Reviews.Count(r => r.Rating == 1)));
            CreateMap<Tour, ReadTourBasicDTO>();
            CreateMap<CreateTourDTO, Tour>()
                .ForMember(dest => dest.TourImages, opt => opt.Ignore());
            CreateMap<UpdateTourDTO, Tour>()
                .ForMember(dest => dest.TourImages, opt => opt.Ignore());
            CreateMap<TourImage, ReadTourImageDTO>();
        }
    }
}
