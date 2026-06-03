using AIAPI.DTOs;
using FluentValidation;
using StayHub.Common.Localization;

namespace AIAPI.Validations;

public class ChatRequestValidator : AbstractValidator<ChatRequestDTO>
{
    public ChatRequestValidator(ValidationLocalizer v)
    {
        RuleFor(x => x.Message)
            .NotEmpty().WithMessage(v.Get("Message is required."))
            .MinimumLength(2).WithMessage(v.Get("Message must be at least 2 characters."))
            .MaximumLength(2000).WithMessage(v.Get("Message cannot exceed 2000 characters."));
    }
}

public class NaturalLanguageSearchRequestValidator : AbstractValidator<NaturalLanguageSearchRequestDTO>
{
    public NaturalLanguageSearchRequestValidator(ValidationLocalizer v)
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage(v.Get("Query is required."))
            .MinimumLength(2).WithMessage(v.Get("Query must be between 2 and 1000 characters."))
            .MaximumLength(1000).WithMessage(v.Get("Query must be between 2 and 1000 characters."));
        RuleFor(x => x.Top).InclusiveBetween(1, 50).WithMessage(v.Get("Top must be between 1 and 50."));
        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage(v.Get("MinPrice cannot be greater than MaxPrice."));
        RuleFor(x => x)
            .Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate <= x.EndDate)
            .WithMessage(v.Get("StartDate cannot be after EndDate."));
        RuleFor(x => x.GroupSize)
            .InclusiveBetween(1, 500)
            .When(x => x.GroupSize.HasValue);
    }
}

public class TourConsultationRequestValidator : AbstractValidator<TourConsultationRequestDTO>
{
    public TourConsultationRequestValidator(ValidationLocalizer v)
    {
        RuleFor(x => x.Top).InclusiveBetween(1, 30).WithMessage(v.Get("Top must be between 1 and 30."));
        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage(v.Get("MinPrice cannot be greater than MaxPrice."));
        RuleFor(x => x)
            .Must(x => !x.PreferredStartDate.HasValue || !x.PreferredEndDate.HasValue || x.PreferredStartDate <= x.PreferredEndDate)
            .WithMessage(v.Get("PreferredStartDate cannot be after PreferredEndDate."));
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.City) || !string.IsNullOrWhiteSpace(x.Country) ||
                       x.CategoryId.HasValue || x.MinPrice.HasValue || x.MaxPrice.HasValue ||
                       x.DurationDays.HasValue || !string.IsNullOrWhiteSpace(x.TravelStyle))
            .WithMessage(v.Get("Provide at least one consultation criterion (city, country, budget, duration, category, or travel style)."));
    }
}

public class LogInteractionRequestValidator : AbstractValidator<LogInteractionRequestDTO>
{
    public LogInteractionRequestValidator(ValidationLocalizer v)
    {
        RuleFor(x => x.TourId).GreaterThan(0).WithMessage(v.Get("TourId must be greater than 0"));
        RuleFor(x => x.InteractionType)
            .Must(t => new[] { "view", "click", "wishlist", "booking", "chat_recommend" }.Contains(t))
            .WithMessage(v.Get("Invalid interaction type."));
    }
}

public class TourPreferenceQuestionnaireValidator : AbstractValidator<TourPreferenceQuestionnaireDTO>
{
    private static readonly string[] ValidInterests =
        ["beach", "culture", "nature", "food", "adventure", "relax", "photography", "city", "river"];

    public TourPreferenceQuestionnaireValidator(ValidationLocalizer v)
    {
        RuleFor(x => x.CompanionType)
            .Must(ct => new[] { TravelCompanionTypes.Solo, TravelCompanionTypes.Couple, TravelCompanionTypes.Family, TravelCompanionTypes.Group }.Contains(ct))
            .WithMessage(v.Get("Invalid companion type."));

        RuleFor(x => x.NationalityType)
            .Must(nt => nt is TravelerNationalityTypes.Vietnamese or TravelerNationalityTypes.Foreigner)
            .WithMessage(v.Get("Invalid nationality type."));

        RuleFor(x => x.PreferredStartDate)
            .Must(d => d.Date >= DateTime.UtcNow.Date.AddDays(-1))
            .WithMessage(v.Get("PreferredStartDate cannot be too far in the past."));

        RuleFor(x => x)
            .Must(x => !x.PreferredEndDate.HasValue || x.PreferredEndDate.Value.Date >= x.PreferredStartDate.Date)
            .WithMessage(v.Get("PreferredEndDate cannot be before PreferredStartDate."));

        RuleFor(x => x.TravelInterests)
            .NotEmpty()
            .Must(list => list.All(i => ValidInterests.Contains(i, StringComparer.OrdinalIgnoreCase)))
            .WithMessage(v.Get("TravelInterests contains invalid value."));

        RuleFor(x => x.Top).InclusiveBetween(1, 30).WithMessage(v.Get("Top must be between 1 and 30."));

        RuleFor(x => x.ChildrenCount)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.HasChildren)
            .WithMessage(v.Get("ChildrenCount is required when HasChildren is true."));

        RuleFor(x => x.ElderlyCount)
            .NotNull()
            .GreaterThan(0)
            .When(x => x.HasElderly)
            .WithMessage(v.Get("ElderlyCount is required when HasElderly is true."));
    }
}
