using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;

namespace SocialAPI.DTOs;

public class MomentCreateDtoValidator : AbstractValidator<MomentCreateDto>
{
    public MomentCreateDtoValidator()
    {
        RuleFor(x => x.Image)
            .NotNull().WithMessage("Image is required.")
            // ✨ Bổ sung gif, webp vào thông báo lỗi
            .Must(IsValidImage).WithMessage("Only .jpg, .jpeg, .png, .gif, and .webp extensions are allowed.")
            .Must(IsUnder5MB).WithMessage("Image size cannot exceed 5MB.");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId must be greater than 0.");

        RuleFor(x => x.ScheduleId)
            .GreaterThan(0).WithMessage("ScheduleId must be greater than 0.");

        RuleFor(x => x.Caption)
            // ✨ Đồng bộ giới hạn 500 ký tự giống hệt Frontend
            .MaximumLength(500).WithMessage("Caption cannot exceed 500 characters.")
            .Matches(@"^[^<>]*$").WithMessage("HTML tags are not allowed.")
            .When(x => !string.IsNullOrEmpty(x.Caption));

        RuleFor(x => x.Lat)
            .InclusiveBetween(-90.0, 90.0).WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Lng)
            .InclusiveBetween(-180.0, 180.0).WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.Privacy)
            .NotEmpty().WithMessage("Privacy is required.")
            .Must(p => p == "Public" || p == "Private" || p == "Friend").WithMessage("Privacy must be 'Public', 'Private', or 'Friend'.");
    }

    private bool IsValidImage(IFormFile file)
    {
        if (file == null) return false;


        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();


        return allowedExtensions.Contains(extension) && file.ContentType.StartsWith("image/");
    }

    private bool IsUnder5MB(IFormFile file) =>
        file != null && file.Length <= 5 * 1024 * 1024;
}