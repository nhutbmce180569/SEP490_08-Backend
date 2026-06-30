using AutoMapper;
using TourAPI.DTOs;
using TourAPI.Models;

namespace TourAPI.Mappers
{
    public class PromotionProfile : Profile
    {
        public PromotionProfile()
        {
            CreateMap<Promotion, ReadPromotionDTO>();
            CreateMap<CreatePromotionDTO, Promotion>();
            CreateMap<UpdatePromotionDTO, Promotion>();
        }
    }
}
