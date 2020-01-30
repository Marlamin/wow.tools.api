using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace wow.tools.api.Models
{
    public class Database : File
    {
        /// <summary>
        /// Table name.
        /// </summary>
        public string Name { get; set; }
    }
}