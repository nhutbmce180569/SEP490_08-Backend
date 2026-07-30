using AuthAPI.Validations;
using System.ComponentModel.DataAnnotations;

namespace AuthAPI.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = null!;
    }

    public class GoogleLoginDTO
    {
        [Required(ErrorMessage = "Google IdToken is required.")]
        public string IdToken { get; set; } = string.Empty;

        [MaxLength(15, ErrorMessage = "PhoneNumberMax15Chars")]
        [RegularExpression(@"^[0-9+()\- ]{8,15}$", ErrorMessage = "InvalidPhoneNumberFormat")]
        public string? PhoneNumber { get; set; }
    }

    public class RefreshTokenRequestDTO
    {
        [Required(ErrorMessage = "Refresh token is required.")]
        public string RefreshToken { get; set; } = null!;
    }

    public class LoginResponseDTO
    {
        public bool RequirePhoneNumber { get; set; }
        public UserResponseDTO User { get; set; } = null!;
        public string Token { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }

    public class UserResponseDTO
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;

        public string? AvatarUrl { get; set; }

        public string? Provider { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public DateTime? LastOnline { get; set; }
        public bool RequirePasswordChange { get; set; }
        public bool HasCompletedTour { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class UpdateProfileDTO
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
        [RegularExpression(@"^[a-zA-Z0-9\sÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ]+$", ErrorMessage = "FullNameCannotContainSpecialCharacters")]
        public string FullName { get; set; } = null!;

        [Phone(ErrorMessage = "Invalid phone number format.")]
        public string? PhoneNumber { get; set; }

        public string? Gender { get; set; }

        [NotFutureDate(ErrorMessage = "Date of birth cannot be in the future.")]
        public DateOnly? DateOfBirth { get; set; }

        [AllowedImageExtensions(new string[] { ".jpg", ".jpeg", ".png", ".gif" }, ErrorMessage = "Avatar must be an image file (.jpg, .jpeg, .png, .gif).")]
        public IFormFile? AvatarFile { get; set; }
    }

    public class RegisterDTO
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
        [RegularExpression(@"^[a-zA-Z0-9\sÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ]+$", ErrorMessage = "FullNameCannotContainSpecialCharacters")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Phone Number is required.")]
        [StringLength(15, MinimumLength = 8, ErrorMessage = "PhoneNumberMax15Chars")]
        [RegularExpression(@"^[0-9+()\- ]{8,15}$", ErrorMessage = "InvalidPhoneNumberFormat")]
        public string PhoneNumber { get; set; } = null!;

        [Required(ErrorMessage = "VerificationCodeRequired")]
        public string OtpCode { get; set; } = null!;
    }

    public class SendRegisterOtpDTO
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Full Name is required.")]
        [RegularExpression(@"^[a-zA-Z0-9\sÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚĂĐĨŨƠàáâãèéêìíòóôõùúăđĩũơƯĂẠẢẤẦẨẪẬẮẰẲẴẶẸẺẼỀỀỂưăạảấầẩẫậắằẳẵặẹẻẽềềểỄỆỈỊỌỎỐỒỔỖỘỚỜỞỠỢỤỦỨỪễệỉịọỏốồổỗộớờởỡợụủứừỬỮỰỲỴÝỶỸửữựỳỵỷỹ]+$", ErrorMessage = "FullNameCannotContainSpecialCharacters")]
        public string FullName { get; set; } = null!;
    }

    public class FacebookLoginDTO
    {
        [Required(ErrorMessage = "Facebook AccessToken is required.")]
        public string AccessToken { get; set; } = string.Empty;
    }

    public class FacebookUserDTO
    {
        public string Id { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class ChangePasswordDTO
    {
        [Required(ErrorMessage = "Old password is required.")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "New password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordDTO
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = null!;
    }

    public class ForgotPasswordResultDTO
    {
        public bool IsRateLimited { get; set; }
        public int RetryAfterSeconds { get; set; }
        public bool IsSocialAccount { get; set; }
        public string? Provider { get; set; }
    }

    public class VerifyResetOtpDTO
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Verification code is required.")]
        public string Code { get; set; } = null!;
    }

    public class ResetPasswordDTO
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = null!;

        public string? Code { get; set; }
        public string? ResetToken { get; set; }

        [Required(ErrorMessage = "New password is required.")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
            ErrorMessage = "New password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        public string NewPassword { get; set; } = null!;
    }
}
