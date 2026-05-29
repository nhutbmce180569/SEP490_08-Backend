using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace SocialAPI.Services;

public interface ICloudStorageService
{
  
    Task<string> UploadImageAsync(IFormFile file, string folderName = "stayhub/social");
}