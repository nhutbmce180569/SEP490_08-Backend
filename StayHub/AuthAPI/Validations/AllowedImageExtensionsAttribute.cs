using System.ComponentModel.DataAnnotations;
using StayHub.Common.Localization;

namespace AuthAPI.Validations
{
    public class AllowedImageExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public AllowedImageExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
            ErrorMessage = "Invalid file format. Only image files are allowed.";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
            {
                return ValidationResult.Success;
            }

            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName).ToLower();

                if (!_extensions.Contains(extension))
                {
                    var key = string.IsNullOrWhiteSpace(ErrorMessage)
                        ? "Invalid file format. Only image files are allowed."
                        : ErrorMessage;
                    return new ValidationResult(ValidationAttributeHelper.Localize(validationContext, key));
                }
            }

            return ValidationResult.Success;
        }
    }
}
