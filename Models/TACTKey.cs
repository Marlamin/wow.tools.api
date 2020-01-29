using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wow.tools.api.Models
{
    public class TACTKey
    {
        /// <summary>
        /// ID from TactKey.db2.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// Hex representation of 8-byte lookup from TactKeyLookup.db2.
        /// </summary>
        public string Lookup { get; set; }

        /// <summary>
        /// Hex representation of 16-byte key from TactKey.db2 and/or hotfixes.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Manually set description of what this key encrypts.
        /// </summary>
        public string Description { get; set; }
    }
}
