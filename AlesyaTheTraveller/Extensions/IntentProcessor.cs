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
                // if something goes wrong, return tomorrow
                var dateToReturn = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");

                var outbountDate = intnt.Entities.FirstOrDefault(x => x.Type == "builtin.datetimeV2.date");

                if (outbountDate == null)
                    return dateToReturn;

                var parseDateSuccess = DateTime.TryParseExact(outbountDate.Value, "MMMM dd", 
                    null, System.Globalization.DateTimeStyles.None, 
                    out var parsedDate);

                // another try with date first
                parseDateSuccess = DateTime.TryParseExact(outbountDate.Value, "dd MMMM",
                    null, System.Globalization.DateTimeStyles.None,
                    out parsedDate);

                return !parseDateSuccess ? dateToReturn : parsedDate.ToString("yyyy-MM-dd");
            }

            var intentParams = new Dictionary<string, string>();
            var intent = JsonConvert.DeserializeObject<Intent>(intentJson);

            var intentType = intent.TopScoringIntent.Intent;
            intentParams.Add("--type", intentType);

            if (intentType == "Interaction")
            {
                intentParams.Add("item", intent.Entities.First(x => x.Type == "item").Value);
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
                                var query = intent.Entities
                                    .Where(x => x.StartIndex == item.Key + 2 && x.Type == "Places.DestinationAddress")
                                    .First().Value;

                                if (item.Value == PlaceQualifierDirection.To)
                                {
                                    // will be used for hotels search and stuff
                                    // not a part of flight query
                                    intentParams["--destination"] = query;
                                }

                                var countryServiceCode = _cache.GetLocation(query).CountryCode;
                                var country = _cache.GetCountryByCode(countryServiceCode);

                                var places = await _dataService.GetPlacesList(query);

                                if (places.Any())
                                {
                                    intentParams.Add(item.Value == PlaceQualifierDirection.From ? "originPlace" : "destinationPlace",
                                        places.FirstOrDefault(x => x.CountryName == country.Name)?.PlaceId ?? places.First().PlaceId);
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
    }
}
