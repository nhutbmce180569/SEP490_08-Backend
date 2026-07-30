using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TourAPI.DTOs
{
    public class ReadReviewDTO
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int TourId { get; set; }
        public string? TourName { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAvatar { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public bool IsHidden { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<ReadReviewReplyDTO>? Replies { get; set; }
        public bool CanEdit => CreatedAt.HasValue && (DateTime.UtcNow - CreatedAt.Value).TotalHours < 24;
    }

    public class CreateReviewDTO
    {
        [Required(ErrorMessage = "CustomerId is required.")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "TourId is required.")]
        public int TourId { get; set; }

        [Required(ErrorMessage = "Rating is required.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")] 
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }

    public class UpdateReviewDTO
    {
        [Required]
        public int CustomerId { get; set; } 

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Comment is required.")]
        [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
        public string Comment { get; set; } = null!;
    }

    public class UserBatchApiResponse
    {
        public string? Message { get; set; }
        public List<UserShortDto>? Data { get; set; }
    }

    public class UserShortDto
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class PagedResult<T>
    {
        public int TotalCount { get; set; }
        public IEnumerable<T> Items { get; set; } = new List<T>();
    }
}
