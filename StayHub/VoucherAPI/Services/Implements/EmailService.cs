using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Utils;
using VoucherAPI.Services;

namespace VoucherAPI.Services.Implements
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, Dictionary<string, byte[]>? inlineImages = null)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(
                emailSettings["SenderName"],
                emailSettings["SenderEmail"]));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = body };

            if (inlineImages != null && inlineImages.Count > 0)
            {
                foreach (var img in inlineImages)
                {
                    var image = builder.LinkedResources.Add(img.Key, img.Value);
                    image.ContentId = MimeUtils.GenerateMessageId();
                    builder.HtmlBody = builder.HtmlBody.Replace($"cid:{img.Key}", $"cid:{image.ContentId}");
                }
            }

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(
                emailSettings["Host"],
                int.Parse(emailSettings["Port"]!),
                MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(
                emailSettings["Username"],
                emailSettings["Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
