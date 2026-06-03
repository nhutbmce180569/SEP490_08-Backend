using FluentValidation;
using StayHub.Common.Localization;

namespace SocialAPI.DTOs;

public class CommentRequestDtoValidator : AbstractValidator<CommentRequestDto>
{
    public CommentRequestDtoValidator(ValidationLocalizer v)
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage(v.Get("UserId must be greater than 0."));

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage(v.Get("Comment cannot be empty."))
            .Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage(v.Get("Comment cannot contain only whitespace."))
            .MaximumLength(500).WithMessage(v.Get("Comment cannot exceed 500 characters."))
            .Matches(@"^[^<>]*$").WithMessage(v.Get("HTML tags are not allowed."));
    }
}
