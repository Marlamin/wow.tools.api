using System;
using System.Collections.Generic;

namespace wow.tools.api.Models
{
    public class Build
    {
        public Version version;
        public Product product;

        public DateTime buildDate;

        public Dictionary<Config.ConfigType, Config> configs;

        public Dictionary<Manifest.ManifestType, Manifest> manifests;
    }
}