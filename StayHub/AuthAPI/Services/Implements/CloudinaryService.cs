using AuthAPI.DTOs;
using AuthAPI.Services;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace AuthAPI.Services.Implements
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

        public async Task<ImageUploadResult> UploadImageAsync(IFormFile file, string folderName = "StayHub_General")
        {
            var uploadResult = new ImageUploadResult();

            if (file != null && file.Length > 0)
            {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = folderName,
                    Transformation = new Transformation()
                        .Width(1280)          // Giới hạn chiều rộng tối đa (VD: 1280px)
                        .Height(1280)         // Giới hạn chiều cao tối đa
                        .Crop("limit")        // Chỉ thu nhỏ nếu ảnh gốc lớn hơn kích thước trên (không làm méo ảnh)
                        .Quality("auto:eco")  // "auto:eco" giảm chất lượng mạnh hơn "auto" thông thường để tiết kiệm
                        .FetchFormat("auto")  // Tự động chuyển sang chuẩn nén tốt nhất (WebP/AVIF)
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

        // Hàm mới dùng RawUploadParams để xử lý PDF, DOCX
        public async Task<string> UploadFileAsync(IFormFile file, string folderName = "StayHub_GiayPhep")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is invalid or empty.");

            using var stream = file.OpenReadStream();

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderName
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception("Error uploading to Cloudinary: " + uploadResult.Error.Message);
            }

            return uploadResult.SecureUrl.ToString();
        }
    }
}