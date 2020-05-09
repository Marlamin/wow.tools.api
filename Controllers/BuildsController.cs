using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wow.tools.api.Models;

namespace wow.tools.api.Controllers
{
    using BuildsResponse = ActionResult<List<Build>>;

    [ApiController]
    [Route("[controller]")]
    public class BuildsController : ControllerBase
    {

        [HttpGet("")]
        public ActionResult<Build> Get(Version version) => throw new NotImplementedException();

        [HttpGet("{branch}")]
        public BuildsResponse List(Project branch) => throw new NotImplementedException();

        [HttpGet("{branch}/{environment}")]
        public BuildsResponse List(Project branch, Models.Environment environment) => throw new NotImplementedException();

        [HttpGet("current")]
        public BuildsResponse Current() => throw new NotImplementedException();

        [HttpGet("{build}")]
        public async Task<Build> GetBuildByBuildConfig(string buildConfig) => throw new NotImplementedException();

        [HttpGet("{build}/files")]
        public List<uint> Files(string build, string includeTypes = "")
        {
            return new List<uint>();
        }

        [HttpGet("{build}/databases")]
        public BuildsResponse Databases(string build) => throw new NotImplementedException();
    }
}