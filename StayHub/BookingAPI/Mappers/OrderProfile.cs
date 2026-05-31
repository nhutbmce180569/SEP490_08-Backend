using AutoMapper;
using BookingAPI.DTOs;
using BookingAPI.Models;

namespace BookingAPI.Mappers
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<CreateOrderDTO, Order>()
                .ForMember(dest => dest.OrderDetails, opt => opt.Ignore())
                .ForMember(dest => dest.Tickets, opt => opt.Ignore());
            CreateMap<CreateTicketDTO, Ticket>();
            CreateMap<OrderDetail, ReadOrderDetailDTO>();
            CreateMap<Order, ReadOrderDTO>()
                .ForMember(dest => dest.TicketCount, opt => opt.MapFrom(src => src.TotalQuantity));

        }
    }
}
