namespace BookingAPI.Services
{
    public sealed class EmailInlineImage
    {
        public EmailInlineImage(string contentId, string fileName, string contentType, byte[] content)
        {
            ContentId = contentId;
            FileName = fileName;
            ContentType = contentType;
            Content = content;
        }

        public string ContentId { get; }

        public string FileName { get; }

        public string ContentType { get; }

        public byte[] Content { get; }
    }

    public interface IEmailService
    {
        Task SendEmailAsync(
            string toEmail,
            string subject,
            string body,
            IEnumerable<EmailInlineImage>? inlineImages = null);
    }
}
