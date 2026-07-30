using AutoMapper;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Mappers
{
    public class TourScheduleStaffProfile : Profile
    {
        public TourScheduleStaffProfile()
        {
            CreateMap<CreateTourScheduleStaffDTO, TourScheduleStaff>();
            CreateMap<UpdateTourScheduleStaffDTO, TourScheduleStaff>();
            CreateMap<TourScheduleStaff, ReadTourScheduleStaffDTO>();
        }
    }
}