using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wow.tools.api.Models
{
    public class TACTKey
    {
        public uint ID { get; set; }
        public string Lookup { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
    }
}
