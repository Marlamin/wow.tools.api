using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using wow.tools.api.Models;

namespace wow.tools.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TACTKeysController : ControllerBase
    {
        /// <summary>
        /// Lists all currently known TACTKeys.
        /// </summary>
        [HttpGet("/tactkeys")]
        public async Task<ActionResult<List<TACTKey>>> List()
        {
            var tactKeyList = new List<TACTKey>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
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

        /// <summary>
        /// Lists fileDataIDs that have been/are encrypted by a given TACTKey.
        /// </summary>
        [HttpGet("{tactKeyID}/files")]
        public ActionResult<List<int>> EncryptedFilesByKeyID(int tactKeyID) => throw new NotImplementedException();
    }
}