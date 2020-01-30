using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace wow.tools.api.Models
{
    public class File
    {
        /// <summary>
        /// FileDataID as given by root file or other official sources.
        /// </summary>
        public int FileDataID { get; set; }

        /// <summary>
        /// Hex representation of 8-byte lookup, if known.
        /// </summary>
        public int Lookup { get; set; }

        /// <summary>
        /// Filename of the file, if known.
        /// </summary>
        public int Filename { get; set; }

        /// <summary>
        /// Whether or not the filename is official (true) or named by the community (false).
        /// </summary>
        public bool IsOfficialFilename { get; set; }
    }
}