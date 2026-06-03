using FluentValidation;
using StayHub.Common.Localization;

namespace SocialAPI.DTOs;

public class FriendRequestDtoValidator : AbstractValidator<FriendRequestDto>
{
    public FriendRequestDtoValidator(ValidationLocalizer v)
    {
        RuleFor(x => x.ReceiverId)
            .GreaterThan(0).WithMessage(v.Get("ReceiverId must be greater than 0."));
    }
}
