using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace wow.tools.api.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Environment
    {
        beta,
        ptr,
        live
    }
}