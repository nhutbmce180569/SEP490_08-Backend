namespace VoucherAPI.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, Dictionary<string, byte[]>? inlineImages = null);
    }
}
