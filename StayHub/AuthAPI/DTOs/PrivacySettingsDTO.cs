using System.ComponentModel.DataAnnotations;

namespace AuthAPI.DTOs
{
    public class PrivacySettingsDto
    {
        public bool? LocPrivacy { get; set; }
        public bool? MomentPrivacy { get; set; }
    }
}