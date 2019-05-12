using AlesyaTheTraveller.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Entities
{
    public class Intent
    {
        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("topScoringIntent")]
        public TopScoringIntent TopScoringIntent { get; set; }

        [JsonProperty("entities")]
        public Entity[] Entities { get; set; }
    }

    public class TopScoringIntent
    {
        [JsonProperty("intent")]
        public string Intent { get; set; }

        [JsonProperty("score", NullValueHandling = NullValueHandling.Ignore)]
        public double? Score { get; set; }
    }

    public class Entity
    {
        [JsonProperty("entity")]
        public string Value { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("startIndex", NullValueHandling = NullValueHandling.Ignore)]
        public int StartIndex { get; set; }

        [JsonProperty("endIndex", NullValueHandling = NullValueHandling.Ignore)]
        public int EndIndex { get; set; }

        [JsonProperty("resolution", NullValueHandling = NullValueHandling.Ignore)]
        public Resolution Resolution { get; set; }

        [JsonProperty("score", NullValueHandling = NullValueHandling.Ignore)]
        public double? Score { get; set; }
    }

    public class Resolution
    {
        [JsonProperty("values")]
        [JsonConverter(typeof(ResolutionValuesConverter))]
        public ResolutionData[] DataValues { get; set; }
    }

    public class ResolutionData
    {
        [JsonProperty("timex", NullValueHandling = NullValueHandling.Ignore)]
        public string TimeX{ get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
        public string Value { get; set; }

        [JsonIgnore]
        public IEnumerable<string> ValuesArray { get; set; }
    }
}
