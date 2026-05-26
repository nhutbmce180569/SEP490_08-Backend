using AutoMapper;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Mappers
{
    public class TourScheduleItineraryProfile : Profile
    {
        public TourScheduleItineraryProfile()
        {
            CreateMap<CreateTourScheduleItineraryDTO, TourScheduleItinerary>();
            CreateMap<UpdateTourScheduleItineraryDTO, TourScheduleItinerary>();
            CreateMap<TourScheduleItinerary, ReadTourScheduleItineraryDTO>();
        }
    }
}
