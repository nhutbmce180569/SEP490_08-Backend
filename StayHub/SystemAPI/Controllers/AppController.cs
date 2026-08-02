using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace SystemAPI.Controllers
{
    [Route("api/app")]
    [ApiController]
    public class AppController : ControllerBase
    {
        [HttpGet("download/apk")]
        public IActionResult DownloadApk()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "StayHub.apk");
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { Message = "APK file not found." });
            }

            var contentType = "application/vnd.android.package-archive";
            return PhysicalFile(filePath, contentType, "StayHub.apk");
        }
    }
}
