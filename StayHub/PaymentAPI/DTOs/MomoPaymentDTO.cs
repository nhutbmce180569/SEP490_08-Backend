namespace PaymentAPI.DTOs
{
    public class MomoPaymentDTO
    {
        public string PaymentUrl { get; set; } = null!;
        public string? Deeplink { get; set; }
        public string? QrCodeUrl { get; set; }
    }
}
