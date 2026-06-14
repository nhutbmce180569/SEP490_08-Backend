using System.ComponentModel.DataAnnotations;

namespace PaymentAPI.DTOs
{
    public class ReadTransactionDTO
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public long Amount { get; set; }
        public string Provider { get; set; } = null!;
        public string? ProviderTxnId { get; set; }
        public string? Status { get; set; }
    }

    public abstract class BaseTransactionDTO
    {
        [Required(ErrorMessage = "OrderId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "OrderId must be greater than 0")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(1, long.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public long Amount { get; set; }

        public string Provider { get; set; } = "VNPay";
        public string? ProviderTxnId { get; set; }
        public string? Status { get; set; } = "Pending";

        [RegularExpression("^(web|mobile)$", ErrorMessage = "ClientType must be web or mobile")]
        public string ClientType { get; set; } = "web";

        public string? CustomerEmail { get; set; }
    }

    public class CreateTransactionDTO : BaseTransactionDTO
    {
    }

    public class UpdateTransactionDTO : BaseTransactionDTO
    {
    }
}
