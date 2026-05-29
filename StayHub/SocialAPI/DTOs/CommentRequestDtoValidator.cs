using FluentValidation;

namespace SocialAPI.DTOs;

public class CommentRequestDtoValidator : AbstractValidator<CommentRequestDto>
{
    public CommentRequestDtoValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId must be greater than 0.");

        RuleFor(x => x.Comment)
            .NotEmpty().WithMessage("Comment cannot be empty.")
            .Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Comment cannot contain only whitespace.")
            .MaximumLength(500).WithMessage("Comment cannot exceed 500 characters.")
            .Matches(@"^[^<>]*$").WithMessage("HTML tags are not allowed.");
    }
}