using AutoMapper;
using BookingAPI.DTOs;
using BookingAPI.Models;

namespace BookingAPI.Mappers
{
    public class OrderProfile : Profile
    {
        public OrderProfile()
        {
            CreateMap<CreateOrderDTO, Order>();
            CreateMap<CreateTicketDTO, Ticket>();
            CreateMap<Order, ReadOrderDTO>();
        }
    }
}
