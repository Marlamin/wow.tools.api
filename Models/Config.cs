using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace wow.tools.api.Models
{
    public class Config
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ConfigType
        {
            build,
            cdn,
            patch,
        }

        internal ConfigType type;

        public string encodingHash;
    }
}