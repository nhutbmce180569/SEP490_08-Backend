using FluentValidation;
using StayHub.Common.Localization;

namespace SocialAPI.DTOs;

public class FriendRequestUpdateDtoValidator : AbstractValidator<FriendRequestUpdateDto>
{
    public FriendRequestUpdateDtoValidator(ValidationLocalizer v)
    {
        RuleFor(x => x.RequestId)
            .GreaterThan(0).WithMessage(v.Get("RequestId must be greater than 0."));

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage(v.Get("Status is required."))
            .Must(status => status != null && (status.Equals("Accepted", StringComparison.OrdinalIgnoreCase) || status.Equals("Declined", StringComparison.OrdinalIgnoreCase)))
            .WithMessage(v.Get("Status must be either 'Accepted' or 'Declined'."));
    }
}
