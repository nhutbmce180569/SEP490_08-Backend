using System;
using System.ComponentModel.DataAnnotations;

namespace AuthAPI.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        public string? PhoneNumber { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool LocPrivacy { get; set; }
        public bool MomentPrivacy { get; set; }
    }
}