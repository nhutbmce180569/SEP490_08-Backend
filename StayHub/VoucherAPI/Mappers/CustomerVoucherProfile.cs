using AutoMapper;
using VoucherAPI.DTOs;
using VoucherAPI.Models;

namespace VoucherAPI.Mappers;

public class CustomerVoucherProfile : Profile
{
    public CustomerVoucherProfile()
    {
        CreateMap<UserVoucher, ReadSavedVoucherDTO>()
            .ForMember(dest => dest.UserVoucherId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.Voucher.Code))
            .ForMember(dest => dest.TourId, opt => opt.MapFrom(src => src.Voucher.TourId))
            .ForMember(dest => dest.DiscountType, opt => opt.MapFrom(src => src.Voucher.DiscountType))
            .ForMember(dest => dest.DiscountValue, opt => opt.MapFrom(src => src.Voucher.DiscountValue))
            .ForMember(dest => dest.MaxDiscountAmount, opt => opt.MapFrom(src => src.Voucher.MaxDiscountAmount))
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.Voucher.StartDate))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.Voucher.EndDate))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Voucher.Description))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.Voucher.IsActive))
            .ForMember(dest => dest.VoucherStatus, opt => opt.MapFrom(src => ResolveVoucherStatus(src.Voucher)))
            .ForMember(dest => dest.TourName, opt => opt.Ignore());
    }

    private static string ResolveVoucherStatus(Voucher voucher)
    {
        var now = DateTime.Now;
        if (now > voucher.EndDate) return "Expired";
        if (!voucher.IsActive) return "Inactive";
        if (voucher.UsedCount >= voucher.AvailableCount) return "Depleted";
        if (now < voucher.StartDate) return "Scheduled";
        return "Active";
    }
}
