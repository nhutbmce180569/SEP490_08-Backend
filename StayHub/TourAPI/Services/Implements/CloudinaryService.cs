using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using TourAPI.Models;

namespace TourAPI.Services.Implements
{
    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folder, string target, int id)
        {
            if (file == null || file.Length == 0)
                return null;

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = $"{target}_{id}",
                AssetFolder = $"stayhub/{folder}",
                Overwrite = true
            };
            var result = await _cloudinary.UploadAsync(uploadParams);

            return result.SecureUrl?.ToString();
        }

        public async Task<string?> UploadGalleryImageAsync(IFormFile file, string folder, string publicId)
        {
            if (file == null || file.Length == 0)
                return null;

            await using var stream = file.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                PublicId = publicId,
                AssetFolder = $"stayhub/{folder}",
                Overwrite = true
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            return result.SecureUrl?.ToString();
        }
        public async Task<bool> DeleteImageAsync(string publicId)
        {
            var result = await _cloudinary.DestroyAsync(
                new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                }
            );

            Console.WriteLine(result.Result);

            return result.Result == "ok";
        }
    }
}
