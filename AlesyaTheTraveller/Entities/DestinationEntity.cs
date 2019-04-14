using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Entities
{
    public enum DestinationType
    {
        Country,
        City
    }

    public class DestinationEntity
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("name_translations")]
        public NameTranslations NameTranslations { get; set; }
    }

    public class NameTranslations
    {
        [JsonProperty("en")]
        public string En { get; set; }
    }
}
