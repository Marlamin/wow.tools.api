using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wow.tools.api.Models
{

    public class FileVersion
    {
        public string BuildConfig { get; set; } // TODO: Return Build?
        public string ContentHash { get; set; }

        /// <summary>
        /// TACT key this file version is encrypted with.
        /// </summary>
        public TACTKey? EncryptedWithKey { get; set; }
    }
}
