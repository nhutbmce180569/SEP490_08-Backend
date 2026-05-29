using FluentValidation;
using System;

namespace SocialAPI.DTOs;

public class FriendRequestUpdateDtoValidator : AbstractValidator<FriendRequestUpdateDto>
{
    public FriendRequestUpdateDtoValidator()
    {
        RuleFor(x => x.RequestId)
            .GreaterThan(0).WithMessage("RequestId must be greater than 0.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(status => status != null && (status.Equals("Accepted", StringComparison.OrdinalIgnoreCase) || status.Equals("Declined", StringComparison.OrdinalIgnoreCase))).WithMessage("Status must be either 'Accepted' or 'Declined'.");
    }
}