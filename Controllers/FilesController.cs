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
    public class FilesController : ControllerBase
    {
        [HttpGet("")]
        public ActionResult<List<File>> List() => throw new NotImplementedException();

        [HttpGet("{fileDataID}")]
        public async Task<File> ByFileDataID(int fileDataID)
        {
            var file = new File();
            using (var connection = new MySqlConnection(SettingsManager.connectionString))
            {
                await connection.OpenAsync();
                using var cmd = new MySqlCommand("SELECT id, lookup, filename, verified FROM wow_rootfiles WHERE id = @id", connection);
                cmd.Parameters.AddWithValue("id", fileDataID);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    file.FileDataID = reader.GetInt32(0);
                    file.Lookup = reader["lookup"] != DBNull.Value ? reader.GetString(1) : null;
                    file.Filename = reader["filename"] != DBNull.Value ? reader.GetString(2) : null;
                    file.IsOfficialFilename = reader.GetBoolean(3);
                }
            }

            return file;
        }

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

        [HttpGet("download/build/{buildConfig}/{fileDataId}")]
        public IActionResult DownloadByFileDataId(int fileDataID, string buildConfig) => throw new NotImplementedException();

        [HttpGet("download/name/{buildConfig}/{fileName}")]
        public IActionResult DownloadByFileName(string fileName, string buildConfig) => throw new NotImplementedException();

        [HttpGet("download/content_hash/{contentHash}")]
        public IActionResult DownloadByContentHash(string contentHash) => throw new NotImplementedException();
    }
}