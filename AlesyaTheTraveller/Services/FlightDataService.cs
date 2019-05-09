using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using AlesyaTheTraveller.Entities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace AlesyaTheTraveller.Services
{
    public interface IFlightDataService
    {
        Task<DestinationEntity[]> GetData(DestinationType type);
        Task<PlaceEntity[]> GetPlacesList(string query);
        Task<string> CreateSession(Dictionary<string, string> requestParams);
        Task<RootObject> PollSessionResults(string sessionId);
        List<FlightData> FormFlightData(RootObject root);
        // get tickets
    }
    
    public class FlightDataService : IFlightDataService
    {
        private IConfiguration _config;
        private HttpClient _client;

        public FlightDataService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<string> CreateSession(Dictionary<string, string> requestParams)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("X-RapidAPI-Host", _config["RapidApi:Host"]);
            _client.DefaultRequestHeaders.Add("X-RapidAPI-Key", _config["RapidApi:Key"]);

            using (var content = new FormUrlEncodedContent(requestParams))
            {
                content.Headers.Clear();
                content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                var uri = _config["RapidApi:Url"] + _config["RapidApi:FlightSearch"] + _config["RapidApi:Version"];
                var response = await _client.PostAsync(uri, content);

                if (response.IsSuccessStatusCode)
                {
                    var segments = response.Headers.Location.Segments;
                    return segments[segments.Length - 1];
                }
                else
                {
                    var a = response.Content.ReadAsStringAsync();
                    // errorka
                }
            }

            return null;
        }

        public async Task<DestinationEntity[]> GetData(DestinationType type)
        {
            var url = "http://api.travelpayouts.com/data/ru/" + 
                (type == DestinationType.Country 
                    ? "countries.json" 
                    : "cities.json");

            var client = new HttpClient();
            var response = await client.GetAsync(url);

            if(!response.IsSuccessStatusCode)
            { 
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<DestinationEntity[]>(json);
        }

        public async Task<PlaceEntity[]> GetPlacesList(string query)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("X-RapidAPI-Host", _config["RapidApi:Host"]);
            _client.DefaultRequestHeaders.Add("X-RapidAPI-Key", _config["RapidApi:Key"]);

            var queryParams = HttpUtility.ParseQueryString(string.Empty);
            queryParams["query"] = query;

            var uri = _config["RapidApi:Url"] + _config["RapidApi:PlaceSearch"] + _config["RapidApi:Version"] + 
                _config["RapidApi:LocaleSettings"] + "?" + queryParams;
            var response = await _client.GetAsync(uri);

            if(!response.IsSuccessStatusCode)
            {
                // error
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<Places>(json).Entities;
        }

        public async Task<RootObject> PollSessionResults(string sessionId)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("X-RapidAPI-Host", _config["RapidApi:Host"]);
            _client.DefaultRequestHeaders.Add("X-RapidAPI-Key", _config["RapidApi:Key"]);

            var uri = _config["RapidApi:Url"] + _config["RapidApi:FlightSearch"] + "uk2/" +
                _config["RapidApi:Version"] + sessionId + "?pageIndex=1";// &pageSize=2";
            var response = await _client.GetAsync(uri);

            if(!response.IsSuccessStatusCode)
            {
                // error
            }

            var json = await response.Content.ReadAsStringAsync();
            var a = JsonConvert.DeserializeObject<RootObject>(json);
            return a;
        }

        public List<FlightData> FormFlightData(RootObject root)
        {
            var itineraries = new List<FlightData>();

            foreach(var itinerary in root.Itineraries)
            {
                var outboundInfo = root.Legs.First(x => x.Id == itinerary.OutboundLegId);

                itineraries.Add(new FlightData
                {
                    CarrierImageUri = root.Carriers.First(x => x.Id == outboundInfo.Carriers[0]).ImageUrl,
                    Origin = root.Places.First(x => x.Id == outboundInfo.OriginStation).Name,
                    Destination = root.Places.First(x => x.Id == outboundInfo.DestinationStation).Name,
                    DepartureTime = outboundInfo.Departure,
                    ArrivalTime = outboundInfo.Arrival,
                    Cost = itinerary.PricingOptions.First().Price,
                    Stops = outboundInfo.Stops.Count(),
                    TicketSellerUri = itinerary.PricingOptions.First().DeeplinkUrl
                });
            }

            return itineraries;
        }
    }
}
