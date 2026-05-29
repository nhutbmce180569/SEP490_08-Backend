using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BookingAPI.Services.Implements
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(
            string toEmail,
            string subject,
            string body,
            IEnumerable<EmailInlineImage>? inlineImages = null)
        {
            var host = _configuration["EmailSettings:Host"];
            var portText = _configuration["EmailSettings:Port"];
            var senderName = _configuration["EmailSettings:SenderName"];
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var username = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("EmailSettings are not configured correctly.");
            }

            if (!int.TryParse(portText, out var port))
            {
                port = 587;
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName ?? "StayHub Support", senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };

            if (inlineImages != null)
            {
                foreach (var image in inlineImages)
                {
                    var resource = bodyBuilder.LinkedResources.Add(image.FileName, image.Content);
                    resource.ContentId = image.ContentId;
                    resource.ContentDisposition = new ContentDisposition(ContentDisposition.Inline);
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            try
            {
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }
    }
}
