using CloudinaryDotNet.Actions;

namespace AuthAPI.Services
{
    public interface ICloudinaryService
    {
        // Thêm tham số folderName vào Interface
        Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folderName = "StayHub_General");
        Task<DeletionResult> DeleteImageAsync(string publicId);
        string? ExtractPublicIdFromUrl(string imageUrl);
        Task<string> UploadFileAsync(IFormFile file, string folderName = "StayHub_GiayPhep");
    }
}
