using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

using System;
using wow.tools.api.Models;

namespace wow.tools.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FilesController : ControllerBase
    {
        [HttpGet("")]
        public ActionResult<List<File>> List() => throw new NotImplementedException();

        [HttpGet("download/build/{buildConfig}/{fileDataId}")]
        public IActionResult DownloadByFileDataId(int fileDataId, string buildConfig) => throw new NotImplementedException();

        [HttpGet("download/name/{buildConfig}/{fileName}")]
        public IActionResult DownloadByFileName(string fileName, string buildConfig) => throw new NotImplementedException();

        [HttpGet("download/content_hash/{contentHash}")]
        public IActionResult DownloadByContentHash(string contentHash) => throw new NotImplementedException();
    }
}