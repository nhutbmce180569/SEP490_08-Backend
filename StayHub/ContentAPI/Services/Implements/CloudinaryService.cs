using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ContentAPI.DTOs;
using Microsoft.Extensions.Options;

namespace ContentAPI.Services.Implements
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            var account = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile file)
        {
            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "StayHub_Banners",
                    Transformation = new Transformation().Quality("auto").FetchFormat("auto")
                };

                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            return uploadResult;
        }

        public async Task<DeletionResult> DeleteImageAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            return await _cloudinary.DestroyAsync(deleteParams);
        }

        // ĐƯA HÀM CẮT CHUỖI VÀO ĐÂY ĐỂ DÙNG CHUNG
        public string? ExtractPublicIdFromUrl(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return null;

            try
            {
                int uploadIndex = imageUrl.IndexOf("upload/");
                if (uploadIndex == -1) return null;

                string afterUpload = imageUrl.Substring(uploadIndex + 7);
                int slashIndex = afterUpload.IndexOf("/");
                if (slashIndex == -1) return null;

                string pathWithExtension = afterUpload.Substring(slashIndex + 1);
                int dotIndex = pathWithExtension.LastIndexOf(".");

                return dotIndex != -1
                    ? pathWithExtension.Substring(0, dotIndex)
                    : pathWithExtension;
            }
            catch
            {
                return null;
            }
        }
    }
}