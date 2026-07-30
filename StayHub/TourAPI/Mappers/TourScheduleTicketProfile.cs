using AutoMapper;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Mappers
{
    public class TourScheduleTicketProfile : Profile
    {
        public TourScheduleTicketProfile()
        {
            CreateMap<CreateTourScheduleTicketDTO, TourScheduleTicket>();
            CreateMap<UpdateTourScheduleTicketDTO, TourScheduleTicket>();
            CreateMap<TourScheduleTicket, ReadTourScheduleTicketDTO>()
                .ForMember(dest => dest.Promotion, opt => opt.MapFrom(src => 
                    src.Promotions.FirstOrDefault(p => p.Status == "Active" && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow) 
                    ?? src.Promotions.FirstOrDefault()));
        }
    }
}
