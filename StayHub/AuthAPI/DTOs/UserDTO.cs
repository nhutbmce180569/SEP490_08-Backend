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
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool RequirePasswordChange { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class AdminCreatedUserDTO
    {
        public ReadUserDTO User { get; set; } = null!;
        public string TemporaryPassword { get; set; } = null!;
    }

    public class CreateUserDTO
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = null!;

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

    public class UserFilterDTO
    {
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }

        public List<string>? Roles { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0.")]
        public int Page { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100.")]
        public int PageSize { get; set; } = 10;
    }
}
