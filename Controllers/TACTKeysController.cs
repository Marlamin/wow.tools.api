using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

using System;
using wow.tools.api.Models;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace wow.tools.api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TACTKeysController : ControllerBase
    {
        [HttpGet("/tactkeys/list")]
        public ActionResult<List<TACTKey>> List()
        {
            var tactKeyList = new List<TACTKey>();

            using(var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT id, keyname, keybytes, description FROM wow_tactkey WHERE keybytes IS NOT NULL";
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        tactKeyList.Add(new TACTKey()
                        {
                            ID = Convert.ToUInt32(reader["id"]),
                            Lookup = reader["keyname"].ToString(),
                            Key = reader["keybytes"].ToString(),
                            Description = reader["description"].ToString()
                        });
                    }
                }
            }

            return tactKeyList;
        }
    }
}