using CloudinaryDotNet.Actions;

namespace ContentAPI.Services
{
    public interface ICloudinaryService
    {
        Task<ImageUploadResult> UploadImageAsync(IFormFile file);
        Task<DeletionResult> DeleteImageAsync(string publicId);
        string? ExtractPublicIdFromUrl(string imageUrl);
    }
}
