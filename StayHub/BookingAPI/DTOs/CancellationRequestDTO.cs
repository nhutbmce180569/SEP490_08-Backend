using System.ComponentModel.DataAnnotations;

namespace BookingAPI.DTOs
{
    public class CreateCancellationRequestDTO
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public string BankName { get; set; } = null!;

        [Required]
        public string AccountNumber { get; set; } = null!;

        [Required]
        public string AccountHolderName { get; set; } = null!;

        [Required]
        public string Reason { get; set; } = null!;
    }

    public class ProcessCancellationDTO
    {
        [Required]
        [RegularExpression("^(Approve|Reject)$", ErrorMessage = "Action must be either 'Approve' or 'Reject'.")]
        public string Action { get; set; } = null!; // "Approve" hoặc "Reject"

        public string? RejectReason { get; set; } // Bắt buộc nếu Action là "Reject"
    }
    public class CancellationRequestListDTO
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public DateTime? RequestedAt { get; set; }
        public long RefundAmount { get; set; }
        public string Status { get; set; } = null!;
    }

    public class CancellationRequestDetailDTO
    {
        public int Id { get; set; }
        public ReadOrderTourDTO? Tour { get; set; }
        public UserProfileResponseDto? Customer { get; set; }
        public string BankName { get; set; } = null!;
        public string AccountNumber { get; set; } = null!;
        public string AccountHolderName { get; set; } = null!;
        public DateTime? RequestedAt { get; set; }
        public long OriginalAmount { get; set; }
        public long CancellationFee { get; set; }
        public int FeePercent { get; set; }
        public long RefundAmount { get; set; }
        public string Reason { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string? RejectReason { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int? ProcessedBy { get; set; }
    }
}
