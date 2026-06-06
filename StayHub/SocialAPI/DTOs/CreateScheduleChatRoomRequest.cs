using System.ComponentModel.DataAnnotations;

namespace SocialAPI.DTOs
{
    public class CreateScheduleChatRoomRequest
    {
        [Required]
        public int ScheduleId { get; set; }

        [Required]
        public string RoomName { get; set; } = null!;
    }
}
