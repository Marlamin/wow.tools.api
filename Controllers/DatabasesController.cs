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
    public class DatabasesController : ControllerBase
    {
        [HttpGet("")]
        public async Task<ActionResult<List<Database>>> List()
        {
            await using var connection = new MySqlConnection(SettingsManager.connectionString);
            await connection.OpenAsync();
            await using var cmd = new MySqlCommand("SELECT displayName, name FROM wow_dbc_tables ORDER BY name ASC", connection);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
                return NotFound();

            var dbList = new List<Database>();

            while (await reader.ReadAsync())
            {
                dbList.Add(new Database
                {
                    DisplayName = reader["displayName"] != DBNull.Value ? reader.GetString(0) : null,
                    Name = reader["name"] != DBNull.Value ? reader.GetString(1) : null
                });
            }

            return dbList;
        }

        [HttpGet("{build}")]
        public async Task<ActionResult<List<Database>>> ListForBuild(string build)
        {
            await using var connection = new MySqlConnection(SettingsManager.connectionString);
            await connection.OpenAsync();

            // Retrieve version ID for build
            await using var versionCommand = new MySqlCommand("SELECT id FROM wow_builds WHERE version = @build", connection);
            versionCommand.Parameters.AddWithValue("build", build);
            await using var versionReader = await versionCommand.ExecuteReaderAsync();

            if (!versionReader.HasRows)
                return NotFound();

            await versionReader.ReadAsync();
            var versionID = versionReader.GetInt32(0);
            await versionReader.CloseAsync();
            
            await using var cmd = new MySqlCommand("SELECT displayName, name FROM wow_dbc_tables WHERE ID in (SELECT tableid FROM wow_dbc_table_versions WHERE versionid = @versionid) ORDER BY name ASC", connection);
            cmd.Parameters.AddWithValue("versionid", versionID);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
                return NotFound();

            var dbList = new List<Database>();

            while (await reader.ReadAsync())
            {
                dbList.Add(new Database
                {
                    DisplayName = reader["displayName"] != DBNull.Value ? reader.GetString(0) : null,
                    Name = reader["name"] != DBNull.Value ? reader.GetString(1) : null
                });
            }

            return dbList;
        } 

        [HttpGet("{databaseName}/versions")]
        public async Task<ActionResult<List<string>>> BuildsForDatabase(string databaseName, bool uniqueOnly = false)
        {
            await using var connection = new MySqlConnection(SettingsManager.connectionString);
            await connection.OpenAsync();

            // Retrieve ID for table
            await using var tableCommand = new MySqlCommand("SELECT id FROM wow_dbc_tables WHERE name = @name", connection);
            tableCommand.Parameters.AddWithValue("name", databaseName);
            await using var tableReader = await tableCommand.ExecuteReaderAsync();

            if (!tableReader.HasRows)
                return NotFound();

            await tableReader.ReadAsync();
            var tableID = tableReader.GetInt32(0);
            await tableReader.CloseAsync();

            var queryString = "SELECT version FROM wow_builds INNER JOIN wow_dbc_table_versions ON wow_dbc_table_versions.versionid=wow_builds.id WHERE tableid = @tableid AND wow_dbc_table_versions.hasDefinition = 1";

            if(uniqueOnly)
                queryString += " GROUP BY contenthash";

            queryString += " ORDER BY LENGTH(version), version DESC";

            await using var cmd = new MySqlCommand(queryString, connection);
            cmd.Parameters.AddWithValue("tableid", tableID);
            await using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.HasRows)
                return NotFound();

            var versionList = new List<string>();
            while (await reader.ReadAsync())
            {
                versionList.Add(reader.GetString(0));
            }
            return versionList;
        }

        [HttpGet("{databaseName}/hotfixes")]
        public async Task<ActionResult<Dictionary<uint, uint>>> HotfixedRowsForDatabase(string databaseName, int build)
        {
            await using var connection = new MySqlConnection(SettingsManager.connectionString);
            await connection.OpenAsync();

            await using var command = new MySqlCommand("SELECT recordID, pushID FROM wow_hotfixes WHERE tableName = @name AND build = @build AND isValid = 1 ORDER BY pushID DESC", connection);
            command.Parameters.AddWithValue("name", databaseName);
            command.Parameters.AddWithValue("build", build);
            await using var reader = await command.ExecuteReaderAsync();

            var hotfixedIDList = new Dictionary<uint, uint>();
            if (!reader.HasRows)
                return hotfixedIDList;

            while (await reader.ReadAsync())
            {
                var recordID = reader.GetUInt32(0);
                var pushID = reader.GetUInt32(1);
                if (!hotfixedIDList.ContainsKey(recordID))
                {
                    hotfixedIDList.Add(recordID, pushID);
                }
            }
            return hotfixedIDList;
        }

        [HttpGet("{buildConfig}/{databaseName}/json")]
        public ActionResult<Database> GetAsJSONForBuild(string buildConfig, string databaseName) => throw new NotImplementedException();

        [HttpGet("{buildConfig}/{databaseName}/csv")]
        public ActionResult<Database> GetAsCSVForBuild(string buildConfig, string databaseName) => throw new NotImplementedException();
    }
}
