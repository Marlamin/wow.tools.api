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
    public class FilesController : ControllerBase
    {
        [HttpGet("")]
        public ActionResult<List<File>> List() => throw new NotImplementedException();

        /// <summary>
        /// Lists details for a specific FileDataID.
        /// </summary>
        /// <response code="200">Returns details for this file</response>
        /// <response code="404">If the FileDataID is unknown</response>        
        [HttpGet("{fileDataID}")]
        public async Task<ActionResult<File>> ByFileDataID(int fileDataID)
        {
            var file = new File();
            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = new MySqlCommand("SELECT id, lookup, filename, type, verified FROM wow_rootfiles WHERE id = @id", connection);
                cmd.Parameters.AddWithValue("id", fileDataID);
                using var reader = await cmd.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return NotFound();
                }

                while (await reader.ReadAsync())
                {
                    file.FileDataID = reader.GetInt32(0);
                    file.Lookup = reader["lookup"] != DBNull.Value ? reader.GetString(1) : null;
                    file.Filename = reader["filename"] != DBNull.Value ? reader.GetString(2) : null;
                    file.Type = reader["type"] != DBNull.Value ? reader.GetString(3) : null;
                    file.IsOfficialFilename = reader.GetBoolean(4);
                }
            }

            return file;
        }

        /// <summary>
        /// Lists known versions for a specific FileDataID.
        /// </summary>
        /// <response code="200">Returns versions for this file</response>
        /// <response code="404">If the FileDataID is unknown or was never shipped (has no versions)</response>    
        [HttpGet("{fileDataID}/versions")]
        public async Task<ActionResult<List<FileVersion>>> ListVersions(int fileDataID)
        {
            var fileVersions = new List<FileVersion>();
            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = new MySqlCommand("SELECT wow_buildconfig.hash, contenthash FROM wow_rootfiles_chashes INNER JOIN wow_buildconfig ON wow_buildconfig.root_cdn=wow_rootfiles_chashes.root_cdn WHERE filedataid = @id", connection);
                cmd.Parameters.AddWithValue("id", fileDataID);
                using var reader = await cmd.ExecuteReaderAsync();

                if (!reader.HasRows)
                {
                    return NotFound();
                }

                while (await reader.ReadAsync())
                {
                    fileVersions.Add(new FileVersion()
                    {
                        BuildConfig = reader.GetString(0),
                        ContentHash = reader.GetString(1)
                    });
                }
            }

            return fileVersions;
        }
    }
}