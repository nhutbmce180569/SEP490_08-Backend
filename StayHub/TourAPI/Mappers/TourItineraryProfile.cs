using AutoMapper;
using System;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Mappers
{
    public class TourItineraryProfile : Profile
    {
        public TourItineraryProfile()
        {
            CreateMap<TourItinerary, ReadTourItineraryDTO>();
            CreateMap<CreateTourItineraryDTO, TourItinerary>();
            CreateMap<UpdateTourItineraryDTO, TourItinerary>();

            CreateMap<TourItinerary, ItineraryLocationDto>()
                .ForMember(dest => dest.StartDuration, opt => opt.MapFrom(src => src.StartDuration.HasValue ? src.StartDuration.Value.ToTimeSpan() : (TimeSpan?)null))
                .ForMember(dest => dest.EndDuration, opt => opt.MapFrom(src => src.EndDuration.HasValue ? src.EndDuration.Value.ToTimeSpan() : (TimeSpan?)null));

            CreateMap<TourScheduleItinerary, ItineraryLocationDto>()
                .ForMember(dest => dest.StartDuration, opt => opt.MapFrom(src => src.StartDuration.HasValue ? src.StartDuration.Value.ToTimeSpan() : (TimeSpan?)null))
                .ForMember(dest => dest.EndDuration, opt => opt.MapFrom(src => src.EndDuration.HasValue ? src.EndDuration.Value.ToTimeSpan() : (TimeSpan?)null));
        }
    }
}