using FluentValidation;

namespace SocialAPI.DTOs;

public class ReactionRequestDtoValidator : AbstractValidator<ReactionRequestDto>
{
    public ReactionRequestDtoValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId must be greater than 0.");

        RuleFor(x => x.MomentId)
            .GreaterThan(0).WithMessage("MomentId must be greater than 0.");

        RuleFor(x => x.IsLike)
            .NotNull().WithMessage("IsLike cannot be null.");
    }
}