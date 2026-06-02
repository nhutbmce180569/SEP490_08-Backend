using AuthAPI.DTOs;
using AuthAPI.Models;
using AutoMapper;

namespace AuthAPI.Mappers
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<RegisterDTO, User>();

            CreateMap<Role, ReadRoleDTO>();

            CreateMap<User, UserResponseDTO>();

            CreateMap<User, ReadUserDTO>()
                 .ForMember(dest => dest.Roles, opt => opt.MapFrom(src => src.Roles.Select(r => r.Name).ToList()));

            CreateMap<CreateUserDTO, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Roles, opt => opt.Ignore());

            CreateMap<UpdateUserDTO, User>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore());

            CreateMap<User, UserSearchResultDto>()
                .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.AvatarUrl))
                .ForMember(dest => dest.RoleIds, opt => opt.MapFrom(src => src.Roles.Select(r => r.Id).ToList()))
                .ForMember(dest => dest.RoleNames, opt => opt.MapFrom(src => src.Roles.Select(r => r.Name).ToList()));
            CreateMap<User, UserProfileDto>();
            CreateMap<UpdateProfileDTO, User>();;
            CreateMap<User, UserProfileResponseDto>();

        }
    }
}
