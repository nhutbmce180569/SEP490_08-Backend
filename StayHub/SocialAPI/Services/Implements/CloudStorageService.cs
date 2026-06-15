using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SocialAPI.Models;
using System;
using System.Threading.Tasks;

namespace SocialAPI.Services.Implements;

public class CloudStorageService : ICloudStorageService
{
    private readonly Cloudinary _cloudinary;

    public CloudStorageService(IOptions<CloudinarySettings> config)
    {
        var account = new Account(config.Value.CloudName, config.Value.ApiKey, config.Value.ApiSecret);
        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folderName = "stayhub/general")
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty or null.");

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folderName,
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
            throw new Exception($"Cloudinary upload error: {uploadResult.Error.Message}");

        return uploadResult.SecureUrl.ToString();
    }
}