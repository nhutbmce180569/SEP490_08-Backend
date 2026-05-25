using System.ComponentModel.DataAnnotations;

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
                    var errorMessage = ErrorMessage ?? $"Only the following file extensions are allowed: {string.Join(", ", _extensions)}";
                    return new ValidationResult(errorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}
