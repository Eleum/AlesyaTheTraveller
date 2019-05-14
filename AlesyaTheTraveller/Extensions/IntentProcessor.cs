using AlesyaTheTraveller.Entities;
using AlesyaTheTraveller.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace AlesyaTheTraveller.Extensions
{
    enum PlaceQualifierDirection
    {
        None,
        From,
        To
    }

    public class LuisConfig
    {
        public string LuisAppUrl { get; }
        public string LuisApiKey { get; }
        public string LuisAppId { get; }

        public LuisConfig(string luisAppUrl, string luisApiKey, string luisAppId)
        {
            LuisAppUrl = luisAppUrl;
            LuisApiKey = luisApiKey;
            LuisAppId = luisAppId;
        }
    }

    public class IntentProcessor
    {
        private readonly LuisConfig _config;
        private readonly IFlightDataCacheService _cache;
        private readonly IFlightDataService _dataService;

        public IntentProcessor(LuisConfig config, IFlightDataCacheService cache, IFlightDataService dataService)
        {
            _config = config;
            _cache = cache;
            _dataService = dataService;
        }

        public async Task<string> GetMessageIntentAsync(string message)
        {
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _config.LuisApiKey);

            queryString["q"] = message;
            queryString["timezoneOffset"] = "0";
            queryString["verbose"] = "false";
            queryString["spellCheck"] = "false";
            queryString["staging"] = "true";

            var uri = _config.LuisAppUrl + _config.LuisAppId + "?" + queryString;
            var response = await client.GetAsync(uri);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                return "ERRORRRRRRRRRRRRRRRRRRRRR";
            }
        }

        public async Task<Dictionary<string, string>> ParseIntent(string intentJson)
        {
            string GetOutboundDate(Intent intnt)
            {
                var format = "yyyy-MM-dd";
                var outbountDate = intnt.Entities.FirstOrDefault(x => x.Type.StartsWith("builtin.datetimeV2"));

                // if something goes wrong, return tomorrow
                var dateToReturn = DateTime.Now.AddDays(1).ToString(format);

                if (outbountDate == null)
                    return dateToReturn;

                DateTime[] dates = null;
                var firstValue = outbountDate.Resolution.DataValues.First();
                
                // get possible dates for user date input
                if (outbountDate.Type.EndsWith("daterange")) // when said 'next week' and so on...
                {
                    dates = new[]
                    {
                        DateTime.ParseExact(firstValue.Start, format, null),
                        DateTime.ParseExact(firstValue.End, format, null)
                    };
                }
                else // other cases
                {
                    dates = new[]
                    {
                        DateTime.ParseExact(firstValue.Value, format, null),
                        DateTime.ParseExact(outbountDate.Resolution.DataValues.Last().Value, format, null)
                    };
                }

                // return first value greater than DateTime.Now
                // if there's no, return tomorrow
                return dates[0] < DateTime.Now
                    ? dates[1] < DateTime.Now
                        ? dateToReturn
                        : dates[1].ToString(format)
                    : dates[0].ToString(format);
            }
            void CheckDestinationConsistency(Entity entity)
            {
                if (entity == null)
                    return;

                var parts = entity.Value.Split(' ');
                if (parts.Length < 2)
                    return;

                var lastWord = parts.Last();
                if (lastWord == "this" || lastWord == "next" ||
                    lastWord == "last" || lastWord == "previous")
                {
                    entity.Value = string.Join(" ", parts.Take(parts.Length-1));
                }
            }

            var intentParams = new Dictionary<string, string>();
            Intent intent = null;
            try
            {
                intent = JsonConvert.DeserializeObject<Intent>(intentJson);
            }
            catch(Exception e)
            {

            }

            var intentType = intent.TopScoringIntent.Intent;
            intentParams.Add("--type", intentType);

            if (intentType == "Interaction")
            {
                var sortType = intent.Entities.FirstOrDefault(x => x.Type == "builtin.number")?.Value;

                //TODO: change to get 'action' entity
                if (!string.IsNullOrEmpty(sortType))
                {
                    intentParams.Add("number", sortType);
                }
                else
                {
                    intentParams.Add("item", intent.Entities.First(x => x.Type == "item").Value);
                }
                
            }
            else if (intentType == "Travelling")
            {
                intentParams.Add("country", "BY");
                intentParams.Add("currency", "BYN");
                intentParams.Add("locale", "ru-RU");
                intentParams.Add("adult", "1");
                intentParams.Add("outboundDate", GetOutboundDate(intent));

                var destinationCount = intent.Entities.Count(x => x.Type == "Places.DestinationAddress");
                if (destinationCount == 0)
                {
                    return null;
                }

                var placeQualifiers = intent.Entities.Where(x => x.Type == "placeQualifier");

                if (destinationCount == 1)
                {
                    var destination = intent.Entities.First(x => x.Type == "Places.DestinationAddress");
                    if (!placeQualifiers.Any())
                    {
                        intentParams.Add("destinationPlace", _cache.GetLocation(destination.Value).Code + "-sky");
                    }
                    else
                    {

                    }
                }
                else
                {
                    if (!intent.Entities.Any(x => x.Type == "placeQualifier"))
                    {
                        var first = false;
                        foreach(var destination in intent.Entities.Where(x => x.Type == "Places.DestinationAddress"))
                        {
                            CheckDestinationConsistency(destination);
                            first = !first;

                            if(!first)
                            {
                                // will be used for hotels search and stuff
                                // not a part of flight query
                                intentParams["--destination"] = destination.Value;
                            }

                            var info = await FetchPlaceInfo(destination.Value);
                            if (info.Item1.Any())
                            {
                                // fix Russia->Россия after St. Petersburg search
                                intentParams.Add(first? "originPlace" : "destinationPlace",
                                    info.Item1.FirstOrDefault(x => (x.CountryName == "Russia" ? "Россия" : x.CountryName) == info.Item2)?.PlaceId ?? 
                                    info.Item1.First().PlaceId);
                            }
                        }
                    }
                    else if (intent.Entities.Count(x => x.Type == "placeQualifier") == 1)
                    {

                    }
                    else
                    {
                        // valid place qualifiers
                        var validPqs = new Dictionary<int, PlaceQualifierDirection>();
                        foreach (var item in placeQualifiers)
                        {
                            // if this place qualifier is before destination address in intent string
                            if (intent.Entities.Any(x => x.StartIndex == item.EndIndex + 2 && x.Type == "Places.DestinationAddress"))
                            {
                                validPqs.Add(
                                    item.EndIndex,
                                    Enum.TryParse(typeof(PlaceQualifierDirection), item.Value, true, out var direction)
                                        ? (PlaceQualifierDirection)direction
                                        : PlaceQualifierDirection.None
                                    );
                            }
                        }

                        foreach (var item in validPqs)
                        {
                            if (item.Value == PlaceQualifierDirection.None)
                            {
                                continue;
                            }
                            else
                            {
                                var queryEntity = intent.Entities
                                    .Where(x => x.StartIndex == item.Key + 2 && x.Type == "Places.DestinationAddress")
                                    .First();

                                CheckDestinationConsistency(queryEntity);

                                if (item.Value == PlaceQualifierDirection.To)
                                    intentParams["--destination"] = queryEntity.Value;
                                
                                var info = await FetchPlaceInfo(queryEntity.Value);
                                if (info.Item1.Any())
                                {
                                    intentParams.Add(item.Value == PlaceQualifierDirection.From ? "originPlace" : "destinationPlace",
                                        info.Item1.FirstOrDefault(x => (x.CountryName == "Russia" ? "Россия" : x.CountryName) == info.Item2)?.PlaceId ?? info.Item1.First().PlaceId);
                                }
                            }
                        }

                        // handling two 'from' pq?

                        // handling 'from minsk from 20.04.2019 to 25.04.2019 to london'
                    }
                }
            }

            return intentParams;
        }

        /// <summary>
        /// Places list, country name for input query
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private async Task<Tuple<PlaceEntity[], string>> FetchPlaceInfo(string query)
        {
            var countryServiceCode = _cache.GetLocation(query).CountryCode;
            var country = _cache.GetCountryByCode(countryServiceCode);
            var places = await _dataService.GetPlacesList(query);

            return new Tuple<PlaceEntity[], string>(places, country.Name);
        }
    }
}
