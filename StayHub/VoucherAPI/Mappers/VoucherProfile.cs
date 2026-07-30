using AutoMapper;
using VoucherAPI.DTOs;
using VoucherAPI.Models;

namespace VoucherAPI.Mappers;

public class VoucherProfile : Profile
{
    public VoucherProfile()
    {
        CreateMap<Voucher, ReadVoucherDTO>()
            .ForMember(dest => dest.AvailableCount, opt => opt.MapFrom(src => src.AvailableCount < src.UsedCount ? src.UsedCount + src.AvailableCount : src.AvailableCount))
            .ForMember(dest => dest.RemainingCount, opt => opt.MapFrom(src => src.AvailableCount < src.UsedCount ? src.AvailableCount : Math.Max(0, src.AvailableCount - src.UsedCount)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ResolveStatus(src)))
            .ForMember(dest => dest.IsCustomerSpecific, opt => opt.MapFrom(src => src.UserVouchers.Any()))
            .ForMember(dest => dest.AssignedCustomerCount, opt => opt.MapFrom(src => src.UserVouchers.Count))
            .ForMember(dest => dest.TourName, opt => opt.Ignore())
            .ForMember(dest => dest.CreatorName, opt => opt.Ignore());

        CreateMap<Voucher, ReadVoucherDetailDTO>()
            .IncludeBase<Voucher, ReadVoucherDTO>()
            .ForMember(dest => dest.AssignedCustomers, opt => opt.MapFrom(src => src.UserVouchers));

        CreateMap<UserVoucher, ReadUserVoucherDTO>()
            .ForMember(dest => dest.UserFullName, opt => opt.Ignore())
            .ForMember(dest => dest.UserEmail, opt => opt.Ignore());

        CreateMap<CreateVoucherDTO, Voucher>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.UsedCount, opt => opt.MapFrom(_ => 0))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.CreatorId, opt => opt.Ignore())
            .ForMember(dest => dest.UserVouchers, opt => opt.Ignore());
    }

    private static string ResolveStatus(Voucher voucher)
    {
        var now = DateTime.UtcNow;
        if (now > voucher.EndDate) return "Expired";
        if (!voucher.IsActive) return "Inactive";
        return "Active";
    }
}
