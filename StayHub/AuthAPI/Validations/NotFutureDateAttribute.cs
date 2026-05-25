using System.ComponentModel.DataAnnotations;

namespace AuthAPI.Validations
{
    public class NotFutureDateAttribute : ValidationAttribute
    {
        public NotFutureDateAttribute()
        {
            ErrorMessage = "Date of birth cannot be in the future.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is DateOnly dateValue)
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                if (dateValue > today)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            else if (value is DateTime dateTimeValue)
            {
                if (dateTimeValue.Date > DateTime.Now.Date)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}
