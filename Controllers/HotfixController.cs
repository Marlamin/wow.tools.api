using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wow.tools.api.Models;

namespace wow.tools.api.Controllers
{
    using HotfixesResponse = ActionResult<List<HotfixFile>>;

    [ApiController]
    [Route("[controller]")]
    public class HotfixController : ControllerBase
    {
        /// <summary>
        /// Lists all currently known TACTKeys.
        /// </summary>
        [HttpGet("/list")]
        public async Task<ActionResult<List<HotfixFile>>> List()
        {
            throw new NotImplementedException();
        }
    }
}