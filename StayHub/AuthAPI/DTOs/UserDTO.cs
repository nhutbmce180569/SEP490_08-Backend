using AuthAPI.Models;
using AuthAPI.Validations;
using System.ComponentModel.DataAnnotations;

namespace AuthAPI.DTOs
{
    public class ReadUserDTO
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }

        public string? Provider { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Status { get; set; }
        public DateTime? LastOnline { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class CreateUserDTO
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "Password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
        public string FullName { get; set; } = null!;

        [AllowedImageExtensions(new string[] { ".jpg", ".jpeg", ".png", ".gif" }, ErrorMessage = "Avatar must be an image file (.jpg, .jpeg, .png, .gif).")]
        public IFormFile? AvatarFile { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; }

        public string? Gender { get; set; }

        [NotFutureDate(ErrorMessage = "Date of birth cannot be in the future.")]
        public DateOnly? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public string Status { get; set; } = "Active";

        public List<int>? RoleIds { get; set; }
    }

    public class UpdateUserDTO
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
        public string FullName { get; set; } = null!;

        [AllowedImageExtensions(new string[] { ".jpg", ".jpeg", ".png", ".gif" }, ErrorMessage = "Avatar must be an image file (.jpg, .jpeg, .png, .gif).")]
        public IFormFile? AvatarFile { get; set; }

        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; }

        public string? Gender { get; set; }

        [NotFutureDate(ErrorMessage = "Date of birth cannot be in the future.")]
        public DateOnly? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(Active|Blocked)$", ErrorMessage = "Status must be either 'Active' or 'Blocked'.")]
        public string? Status { get; set; } = "Active";

        public bool? LocPrivacy { get; set; }
        public bool? MomentPrivacy { get; set; }

        public List<int>? RoleIds { get; set; }
    }

    public class ChangeUserStatusDTO
    {
        [Required(ErrorMessage = "Status is required.")]
        [RegularExpression("^(Active|Blocked)$", ErrorMessage = "Status must be either 'Active' or 'Blocked'.")]
        public string Status { get; set; } = null!;
    }
}