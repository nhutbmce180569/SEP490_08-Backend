using AutoMapper;
using BookingAPI.DTOs;
using BookingAPI.Models;

namespace BookingAPI.Mappings
{
    public class CancellationProfile : Profile
    {
        public CancellationProfile()
        {
            // Map từ Model (Database) sang Detail DTO
            CreateMap<CancellationRequest, CancellationRequestDetailDTO>();

            // Map từ Model (Database) sang List DTO
            CreateMap<CancellationRequest, CancellationRequestListDTO>();
        }
    }
}