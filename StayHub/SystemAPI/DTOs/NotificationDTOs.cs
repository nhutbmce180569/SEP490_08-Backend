using System.ComponentModel.DataAnnotations;

namespace SystemAPI.DTOs
{
    public class CreateNotificationDTO
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;
    }

    public class ReadNotificationDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public bool? IsRead { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
