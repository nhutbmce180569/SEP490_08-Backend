using AutoMapper;
using ContentAPI.DTOs;
using ContentAPI.Models;

namespace ContentAPI.Mappers
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Banner, ReadBannerDTO>();

            CreateMap<CreateBannerDTO, Banner>()
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore()); 

            CreateMap<UpdateBannerDTO, Banner>()
                .ForMember(dest => dest.ImageUrl, opt => opt.Ignore());

            CreateMap<Category, ReadCategoryDTO>();
            CreateMap<CreateCategoryDTO, Category>().ForMember(dest => dest.IconUrl, opt => opt.Ignore());
            CreateMap<UpdateCategoryDTO, Category>().ForMember(dest => dest.IconUrl, opt => opt.Ignore());
        }
    }
}