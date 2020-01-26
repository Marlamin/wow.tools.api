using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace wow.tools.api.Models
{
    public class Manifest
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ManifestType
        {
            encoding,
            install,
            root,
            download,
        }

        internal ManifestType type;
        public string contentHash;

        public string encodingHash;
    }
}