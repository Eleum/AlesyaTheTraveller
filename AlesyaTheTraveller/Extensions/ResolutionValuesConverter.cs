using AlesyaTheTraveller.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Extensions
{
    public class ResolutionValuesConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(ResolutionData));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);

            try
            {
                // try to return class array
                return token.ToObject<ResolutionData[]>();
            }
            catch (Exception)
            {
                // then it's just array of strings
                var data = token.ToObject<IEnumerable<string>>();
                return new ResolutionData[] { new ResolutionData { ValuesArray = data } };
            }

        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
