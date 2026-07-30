namespace PaymentAPI.Models
{
    public class MomoConfig
    {
        public string MomoApiUrl { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
        public string AccessKey { get; set; } = null!;
        public string RedirectUrl { get; set; } = null!;
        public string IpnUrl { get; set; } = null!;
        public string PartnerCode { get; set; } = null!;
        public string RequestType { get; set; } = null!;
    }
}
