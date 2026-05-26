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
            .ForMember(dest => dest.AverageStar,
            opt => opt.MapFrom(src => src.Reviews.Average(x => x.Rating)));
            CreateMap<Tour, ReadTourBasicDTO>();
            CreateMap<CreateTourDTO, Tour>();
            CreateMap<UpdateTourDTO, Tour>();
        }
    }
}
