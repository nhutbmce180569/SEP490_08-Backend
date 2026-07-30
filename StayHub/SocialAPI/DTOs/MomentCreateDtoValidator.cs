using FluentValidation;
using Microsoft.AspNetCore.Http;
using StayHub.Common.Localization;
using System.IO;
using System.Linq;

namespace SocialAPI.DTOs;

public class MomentCreateDtoValidator : AbstractValidator<MomentCreateDto>
{
    public MomentCreateDtoValidator(ValidationLocalizer v)
    {
        RuleFor(x => x.Image)
            .NotNull().WithMessage(v.Get("Image is required."))
            .Must(IsValidImage).WithMessage(v.Get("Only .jpg, .jpeg, .png, .gif, and .webp extensions are allowed."))
            .Must(IsUnder5MB).WithMessage(v.Get("Image size cannot exceed 5MB."));



        RuleFor(x => x.ScheduleId)
            .GreaterThanOrEqualTo(0).WithMessage(v.Get("ScheduleId must be greater than or equal to 0."));

        RuleFor(x => x.Caption)
            .MaximumLength(500).WithMessage(v.Get("Caption cannot exceed 500 characters."))
            .Matches(@"^[^<>]*$").WithMessage(v.Get("HTML tags are not allowed."))
            .When(x => !string.IsNullOrEmpty(x.Caption));

        RuleFor(x => x.Lat)
            .InclusiveBetween(-90.0, 90.0).WithMessage(v.Get("Latitude must be between -90 and 90."));

        RuleFor(x => x.Lng)
            .InclusiveBetween(-180.0, 180.0).WithMessage(v.Get("Longitude must be between -180 and 180."));

        RuleFor(x => x.Privacy)
            .NotEmpty().WithMessage(v.Get("Privacy is required."))
            .Must(p => p == "Public" || p == "Private" || p == "Friend" || p == "Tour")
            .WithMessage(v.Get("Privacy must be 'Public', 'Private', 'Friend', or 'Tour'."));
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
