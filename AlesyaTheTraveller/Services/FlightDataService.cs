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
        Task<GlobalPointEntity[]> GetData(PointType type);
        Task<PlaceEntity[]> GetPlacesList(string query);
        Task<string> CreateSession(Dictionary<string, string> requestParams);
        Task<RootObject> PollSessionResults(string sessionId);
        List<FlightData> FormFlightData(RootObject root);
        Task<LocationEntity[]> GetLocations(string query);
        Task<HotelData[]> GetHotelData(int destinationId, DateTime arrivalDate);
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
            var tries = 3;
            while (tries > 0)
            {
                _client = new HttpClient();
                _client.DefaultRequestHeaders.Add("X-RapidAPI-Host", _config["RapidApi:Host"]);
                _client.DefaultRequestHeaders.Add("X-RapidAPI-Key", _config["RapidApi:Key"]);

                using (var content = new FormUrlEncodedContent(requestParams))
                {
                    content.Headers.Clear();
                    content.Headers.Add("Content-Type", "application/x-www-form-urlencoded");

                    var uri = _config["RapidApi:FlightUrl"] + _config["RapidApi:FlightSearch"] + _config["RapidApi:Version"];
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

                tries--;
            }

            return null;
        }

        public async Task<GlobalPointEntity[]> GetData(PointType type)
        {
            var url = "http://api.travelpayouts.com/data/ru/" + 
                (type == PointType.Country 
                    ? "countries.json" 
                    : "cities.json");

            var client = new HttpClient();
            var response = await client.GetAsync(url);

            if(!response.IsSuccessStatusCode)
            { 
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<GlobalPointEntity[]>(json);
        }

        public async Task<PlaceEntity[]> GetPlacesList(string query)
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("X-RapidAPI-Host", _config["RapidApi:Host"]);
            _client.DefaultRequestHeaders.Add("X-RapidAPI-Key", _config["RapidApi:Key"]);

            var uri = _config["RapidApi:FlightUrl"] + _config["RapidApi:PlaceSearch"] + _config["RapidApi:Version"] + 
                _config["RapidApi:LocaleSettings"] + "?query=" + query.Replace(" ", "+");

            // because it doesn't work for russian language...
            if (query.Contains("petersburg"))
                uri = uri.Replace("ru-RU", "en-US");

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

            var uri = _config["RapidApi:FlightUrl"] + _config["RapidApi:FlightSearch"] + "uk2/" +
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
            if (root.Itineraries == null)
                return null;

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

        public async Task<LocationEntity[]> GetLocations(string query)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-RapidAPI-Host", _config["RapidApi:HotelsHost"]);
            client.DefaultRequestHeaders.Add("X-RapidAPI-Key", _config["RapidApi:Key"]);

            var uri = _config["RapidApi:HotelsUrl"] + _config["RapidApi:Locations"] + "?languagecode=ru&text=" + query.ToLower();
            var response = await client.GetAsync(uri);
            
            if (!response.IsSuccessStatusCode)
            {
                //errorka
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<LocationEntity[]>(json);
        }

        public async Task<HotelData[]> GetHotelData(int destinationId, DateTime arrivalDate)
        {
            var tries = 5;
            while (tries > 0)
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("X-RapidAPI-Host", _config["RapidApi:HotelsHost"]);
                client.DefaultRequestHeaders.Add("X-RapidAPI-Key", _config["RapidApi:Key"]);

                var queryParams = HttpUtility.ParseQueryString(string.Empty);

                queryParams["price_filter_currencycode"] = "BYN";
                queryParams["travel_purpose"] = "leisure";
                queryParams["search_id"] = "none";
                queryParams["order_by"] = "popularity";
                queryParams["languagecode"] = "ru";
                queryParams["search_type"] = "city";
                queryParams["offset"] = "0";
                queryParams["dest_ids"] = destinationId.ToString();
                queryParams["guest_qty"] = "1";
                queryParams["offset"] = "0";
                queryParams["arrival_date"] = arrivalDate.ToString("yyyy-MM-dd");
                queryParams["departure_date"] = arrivalDate.AddDays(5).ToString("yyyy-MM-dd");
                queryParams["room_qty"] = "1";

                var uri = _config["RapidApi:HotelsUrl"] + _config["RapidApi:List"] + "?" + queryParams;
                var response = await client.GetAsync(uri);

                if (!response.IsSuccessStatusCode)
                {
                    //errorka
                    tries--;
                    continue;
                }

                var json = await response.Content.ReadAsStringAsync();

                if (json.Contains("\"code\":\"403\""))
                {
                    await Task.Delay(3000);
                    tries--;
                    continue;
                }

                return JsonConvert.DeserializeObject<RootHotelObject>(json)
                    .HotelData?
                    .Select(x => { UpdateMainPhoto(x); return x; })
                    .ToArray();
            }

            return null;
        }

        private void UpdateMainPhoto(HotelData data)
        {
            // make https and change size
            data.Image = new Uri(data.Image.ToString()
                .Replace("http", "https")
                .Replace("square60", "max300"));
        }
    }
}