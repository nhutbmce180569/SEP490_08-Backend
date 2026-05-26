using AutoMapper;
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
        }
    }
}