using System.ComponentModel.DataAnnotations;

namespace SocialAPI.DTOs
{
    public class AutoAddChatMemberRequest
    {
        [Required]
        public int UserId { get; set; }
    }
}
