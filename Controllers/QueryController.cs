using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace wow.tools.api.Controllers
{
    public class QueryController : Controller
    {
        private class QueryRequest
        {
            public string apiKey { get; set; }
            public string query { get; set; }
        }

        [Route("[controller]")]
        [HttpPost]
        public async Task<string> Index()
        {
            var streader = new StreamReader(Request.Body);
            var rawRequestBody = await streader.ReadToEndAsync();
            var body = JsonSerializer.Deserialize<QueryRequest>(rawRequestBody);

            if (body.apiKey == null || body.apiKey != SettingsManager.apiKey)
                return "Invalid API key";

            if (string.IsNullOrEmpty(rawRequestBody) || rawRequestBody.ToLower().Contains("pragma") || rawRequestBody.ToLower().Contains("union"))
            {
                throw new Exception("Invalid query given");
            }

            var query = new SQLiteCommand();
            query.Connection = Program.cnnOut;
            query.CommandText = body.query;
            query.ExecuteNonQuery();

            SQLiteDataReader reader = query.ExecuteReader();
            var items = new Dictionary<object, Dictionary<string, object>>();
            while (reader.Read())
            {
                var item = new Dictionary<string, object>(reader.FieldCount - 1);
                for (var i = 1; i < reader.FieldCount; i++)
                {
                    item[reader.GetName(i)] = reader.GetValue(i);
                }
                if (item.ContainsKey("ID"))
                {
                    items[item["ID"]] = item;
                }
                else
                {
                    items[reader.GetValue(0)] = item;
                }
            }

            return Newtonsoft.Json.JsonConvert.SerializeObject(items, Newtonsoft.Json.Formatting.Indented);
        }
    }
}
