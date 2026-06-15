namespace PaymentAPI.DTOs
{
    public class PaymentCallbackResultDTO
    {
        public string Status { get; set; } = null!;
        public string? OrderId { get; set; }
        public string ClientType { get; set; } = "web";
    }
}
