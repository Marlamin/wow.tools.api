using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

using System;
using wow.tools.api.Models;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace wow.tools.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TACTKeysController : ControllerBase
    {
        [HttpGet("/tactkeys/list")]
        public async Task<ActionResult<List<TACTKey>>> List()
        {
            var tactKeyList = new List<TACTKey>();

            using(var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new MySqlCommand("SELECT id, keyname, keybytes, description FROM wow_tactkey WHERE keybytes IS NOT NULL", connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tactKeyList.Add(new TACTKey()
                        {
                            ID = reader.GetInt32(0),
                            Lookup = reader.GetString(1),
                            Key = reader.GetString(2),
                            Description = reader.GetString(3),
                        });
                    }
                }
            }

            return tactKeyList;
        }
    }
}