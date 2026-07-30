using AutoMapper;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Mappers
{
    public class TourScheduleProfile : Profile
    {
        public TourScheduleProfile()
        {
            CreateMap<TourSchedule, ReadTourScheduleDTO>();
            CreateMap<CreateTourScheduleDTO, TourSchedule>();
            CreateMap<UpdateTourScheduleDTO, TourSchedule>();
        }
    }
}