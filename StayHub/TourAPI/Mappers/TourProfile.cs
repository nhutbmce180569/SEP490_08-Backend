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
            .ForMember(dest => dest.AverageStar, opt => opt.MapFrom(src => src.Reviews.Where(r => !r.IsHidden).Average(x => x.Rating)))
            .ForMember(dest => dest.TotalReviews, opt => opt.MapFrom(src => src.Reviews.Count(r => !r.IsHidden)))
            .ForMember(dest => dest.FiveStarCount, opt => opt.MapFrom(src => src.Reviews.Count(r => !r.IsHidden && r.Rating == 5)))
            .ForMember(dest => dest.FourStarCount, opt => opt.MapFrom(src => src.Reviews.Count(r => !r.IsHidden && r.Rating == 4)))
            .ForMember(dest => dest.ThreeStarCount, opt => opt.MapFrom(src => src.Reviews.Count(r => !r.IsHidden && r.Rating == 3)))
            .ForMember(dest => dest.TwoStarCount, opt => opt.MapFrom(src => src.Reviews.Count(r => !r.IsHidden && r.Rating == 2)))
            .ForMember(dest => dest.OneStarCount, opt => opt.MapFrom(src => src.Reviews.Count(r => !r.IsHidden && r.Rating == 1)));
            CreateMap<Tour, ReadTourBasicDTO>();
            CreateMap<CreateTourDTO, Tour>();
            CreateMap<UpdateTourDTO, Tour>();
        }
    }
}
