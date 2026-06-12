﻿using AutoMapper;
using BookingAPI.DTOs;
using BookingAPI.Models;

namespace BookingAPI.Mappers
{
    public class TicketProfile : Profile
    {
        public TicketProfile() 
        { 
            CreateMap<Ticket, ReadTicketDTO>()
                .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.OrderDetail.OrderId));
        }
    }
}
