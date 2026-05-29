using FluentValidation;

namespace SocialAPI.DTOs;

public class FriendRequestDtoValidator : AbstractValidator<FriendRequestDto>
{
    public FriendRequestDtoValidator()
    {
        RuleFor(x => x.ReceiverId)
            .GreaterThan(0).WithMessage("ReceiverId must be greater than 0.");
    }
}