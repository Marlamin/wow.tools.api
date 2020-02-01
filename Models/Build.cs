using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace wow.tools.api.Models
{
    public class Build
    {
        public Version version { get; set; }
        public Product product { get; set; }

        public DateTime buildDate { get; set; }

        public Dictionary<Config.ConfigType, Config> configs { get; set; }

        public Dictionary<Manifest.ManifestType, Manifest> manifests { get; set; }
    }
}