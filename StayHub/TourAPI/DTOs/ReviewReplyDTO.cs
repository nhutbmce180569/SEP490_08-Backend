using System;

namespace TourAPI.DTOs
{
    public class CreateReviewReplyDTO
    {
        public int ReviewId { get; set; }
        public string Content { get; set; } = null!;
    }

    public class UpdateReviewReplyDTO
    {
        public string Content { get; set; } = null!;
    }

    public class ReadReviewReplyDTO
    {
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Populated by service: name and avatar of the user who replied (manager/staff)
        public string? UserName { get; set; }
        public string? UserAvatar { get; set; }
    }
}
