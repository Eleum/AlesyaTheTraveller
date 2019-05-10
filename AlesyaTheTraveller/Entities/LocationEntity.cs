using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Entities
{
    public enum PointType
    {
        Country,
        City
    }

    public class GlobalPointEntity
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("name_translations")]
        public NameTranslations NameTranslations { get; set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; set; }
    }

    public class NameTranslations
    {
        [JsonProperty("en")]
        public string En { get; set; }
    }

    public class LocationEntity
    {
        [JsonProperty("dest_id")]
        public int Id { get; set; }

        [JsonProperty("dest_type")]
        public string DestinationType { get; set; }

        [JsonProperty("city_name")]
        public string Name { get; set; }
    }
}
