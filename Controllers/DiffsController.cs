using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

using System;
using wow.tools.api.Models;

namespace wow.tools.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DiffsController : ControllerBase
    {
        [HttpGet("file/{left}/{right}")]
        public ActionResult<Build> DiffFile(String left, String right) => throw new NotImplementedException();

        [HttpGet("database/{left}/{right}")]
        public ActionResult<Build> DiffDatabase(String left, String right) => throw new NotImplementedException();

        [HttpGet("build/{left}/{right}")]
        public ActionResult<Build> DiffBuild(String left, String right) => throw new NotImplementedException();
    }
}