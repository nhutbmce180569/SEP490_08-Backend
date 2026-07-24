using CloudinaryDotNet.Actions;

namespace SystemAPI.Services
{
    public interface ICloudinaryService
    {
        Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folderName = "StayHub_General");
        Task<DeletionResult> DeleteImageAsync(string publicId);
        string? ExtractPublicIdFromUrl(string imageUrl);
    }
}
