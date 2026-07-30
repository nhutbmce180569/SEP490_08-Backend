namespace BookingAPI.Services
{
    public interface IQrCodeService
    {
        byte[] GeneratePngBytes(string content);
    }
}
