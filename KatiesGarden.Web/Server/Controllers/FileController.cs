using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KatiesGarden.Web.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        [HttpGet("/{filePath}")]        
        public IActionResult GetImage(string filePath, int type)
        {            
            var contentType = type == 1 ? "image/jpeg" : "video/mp4";

            //var filePath = Path.Combine(_env.ContentRootPath, "Images", $"{filename}");

            if (Directory.Exists("folderPath"))
            {
                var image = System.IO.File.OpenRead(filePath);
                return File(image, "image/jpeg");
            }

            return new NotFoundObjectResult("Image at file path: '" + filePath + " was not found");
        }        
    }
}
