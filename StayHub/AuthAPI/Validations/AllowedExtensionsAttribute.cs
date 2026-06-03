using System.ComponentModel.DataAnnotations;
using StayHub.Common.Localization;

namespace AuthAPI.Validations
{
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is IFormFile file)
            {
                var extension = Path.GetExtension(file.FileName)?.ToLower();

                if (extension == null || !_extensions.Contains(extension))
                {
                    var message = !string.IsNullOrWhiteSpace(ErrorMessage)
                        ? ValidationAttributeHelper.Localize(validationContext, ErrorMessage)
                        : ValidationAttributeHelper.Localize(
                            validationContext,
                            "Only the following file extensions are allowed: {0}",
                            string.Join(", ", _extensions));
                    return new ValidationResult(message);
                }
            }

            return ValidationResult.Success;
        }
    }
}
