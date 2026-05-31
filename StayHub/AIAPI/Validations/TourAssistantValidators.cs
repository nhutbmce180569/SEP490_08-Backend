using AIAPI.DTOs;
using FluentValidation;

namespace AIAPI.Validations;

public class ChatRequestValidator : AbstractValidator<ChatRequestDTO>
{
    public ChatRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Message is required.")
            .MinimumLength(2).WithMessage("Message must be at least 2 characters.")
            .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters.");
    }
}

public class NaturalLanguageSearchRequestValidator : AbstractValidator<NaturalLanguageSearchRequestDTO>
{
    public NaturalLanguageSearchRequestValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MinimumLength(2).MaximumLength(1000);
        RuleFor(x => x.Top).InclusiveBetween(1, 50);
        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("MinPrice cannot be greater than MaxPrice.");
        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate <= x.EndDate)
            .WithMessage("StartDate cannot be after EndDate.");
        RuleFor(x => x.GroupSize)
            .InclusiveBetween(1, 500)
            .When(x => x.GroupSize.HasValue);
    }
}

public class TourConsultationRequestValidator : AbstractValidator<TourConsultationRequestDTO>
{
    public TourConsultationRequestValidator()
    {
        RuleFor(x => x.Top).InclusiveBetween(1, 30);
        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("MinPrice cannot be greater than MaxPrice.");
        RuleFor(x => x)
            .Must(x => !x.PreferredStartDate.HasValue || !x.PreferredEndDate.HasValue || x.PreferredStartDate <= x.PreferredEndDate)
            .WithMessage("PreferredStartDate cannot be after PreferredEndDate.");
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.City) || !string.IsNullOrWhiteSpace(x.Country) ||
                       x.CategoryId.HasValue || x.MinPrice.HasValue || x.MaxPrice.HasValue ||
                       x.DurationDays.HasValue || !string.IsNullOrWhiteSpace(x.TravelStyle))
            .WithMessage("Provide at least one consultation criterion (city, country, budget, duration, category, or travel style).");
    }
}

public class LogInteractionRequestValidator : AbstractValidator<LogInteractionRequestDTO>
{
    public LogInteractionRequestValidator()
    {
        RuleFor(x => x.TourId).GreaterThan(0);
        RuleFor(x => x.InteractionType)
            .Must(t => new[] { "view", "click", "wishlist", "booking", "chat_recommend" }.Contains(t))
            .WithMessage("Invalid interaction type.");
    }
}

public class TourPreferenceQuestionnaireValidator : AbstractValidator<TourPreferenceQuestionnaireDTO>
{
    private static readonly string[] ValidInterests =
        ["beach", "culture", "nature", "food", "adventure", "relax", "photography", "city", "river"];

    public TourPreferenceQuestionnaireValidator()
    {
        RuleFor(x => x.CompanionType)
            .Must(v => new[] { TravelCompanionTypes.Solo, TravelCompanionTypes.Couple, TravelCompanionTypes.Family, TravelCompanionTypes.Group }.Contains(v))
            .WithMessage("Invalid companion type.");

        RuleFor(x => x.NationalityType)
            .Must(v => v is TravelerNationalityTypes.Vietnamese or TravelerNationalityTypes.Foreigner)
            .WithMessage("Invalid nationality type.");

        RuleFor(x => x.PreferredStartDate)
            .Must(d => d.Date >= DateTime.UtcNow.Date.AddDays(-1))
            .WithMessage("PreferredStartDate cannot be too far in the past.");

        RuleFor(x => x)
            .Must(x => !x.PreferredEndDate.HasValue || x.PreferredEndDate.Value.Date >= x.PreferredStartDate.Date)
            .WithMessage("PreferredEndDate cannot be before PreferredStartDate.");

        RuleFor(x => x.TravelInterests)
            .NotEmpty()
            .Must(list => list.All(i => ValidInterests.Contains(i, StringComparer.OrdinalIgnoreCase)))
            .WithMessage("TravelInterests contains invalid value.");

        RuleFor(x => x.Top).InclusiveBetween(1, 30);

        RuleFor(x => x.ChildrenCount)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.HasChildren)
            .WithMessage("ChildrenCount is required when HasChildren is true.");

        RuleFor(x => x.ElderlyCount)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.HasElderly)
            .WithMessage("ElderlyCount is required when HasElderly is true.");
    }
}
