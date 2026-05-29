using AutoMapper;
using SystemAPI.DTOs;
using SystemAPI.Models;

namespace SystemAPI.Mappers
{
    public class NotificationProfile : Profile
    {
        public NotificationProfile()
        {
            CreateMap<Notification, ReadNotificationDTO>();
            CreateMap<CreateNotificationDTO, Notification>()
                .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => false)) 
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
        }
    }
}
