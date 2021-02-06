using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
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
                            ID = reader["id"] != DBNull.Value ? (int?)reader.GetInt32(0) : null,
                            Lookup = reader.GetString(1),
                            Key = reader["keybytes"] != DBNull.Value ? reader.GetString(2) : "",
                            Description = reader["description"] != DBNull.Value ? reader.GetString(3) : "",
                        });
                    }
                }
            }

            return tactKeyList;
        }

        /// <summary>
        /// Lists fileDataIDs that have been/are encrypted by a given TACTKey.
        /// </summary>
        [HttpGet("{lookup}/files")]
        public async Task<ActionResult<List<int>>> EncryptedFilesByKeyLookup(string lookup)
        {
            var fileList = new List<int>();

            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using (var cmd = new MySqlCommand("SELECT filedataid FROM wow_encrypted WHERE keyname = @lookup", connection))
                {
                    cmd.Parameters.AddWithValue("lookup", lookup);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            fileList.Add(reader.GetInt32(0));
                        }
                    }
                }
            }

            return fileList;
        }
    }
}