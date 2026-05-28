using QRCoder;

namespace BookingAPI.Services.Implements
{
    public class QrCodeService : IQrCodeService
    {
        public byte[] GeneratePngBytes(string content)
        {
            using var generator = new QRCodeGenerator();
            using var qrData = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);

            return qrCode.GetGraphic(12);
        }
    }
}
