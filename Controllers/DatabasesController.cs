using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using wow.tools.api.Models;

namespace wow.tools.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DatabasesController : ControllerBase
    {
        [HttpGet("")]
        public ActionResult<List<Database>> List() => throw new NotImplementedException();

        [HttpGet("{buildConfig}")]
        public ActionResult<Database> ListForBuild(string buildConfig) => throw new NotImplementedException();

        [HttpGet("{databaseName}/versions")]
        public ActionResult<Database> BuildsForDatabase(string databaseName) => throw new NotImplementedException();

        [HttpGet("{buildConfig}/{databaseName}/json")]
        public ActionResult<Database> GetAsJSONForBuild(string buildConfig, string databaseName) => throw new NotImplementedException();

        [HttpGet("{buildConfig}/{databaseName}/csv")]
        public ActionResult<Database> GetAsCSVForBuild(string buildConfig, string databaseName) => throw new NotImplementedException();
    }
}