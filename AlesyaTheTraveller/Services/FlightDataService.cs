using System;
using System.Net.Http;
using System.Threading.Tasks;
using AlesyaTheTraveller.Entities;
using Newtonsoft.Json;

namespace AlesyaTheTraveller.Services
{
    public interface IFlightDataService
    {
        string CreateSession(string intentJson);

        Task<DestinationEntity[]> GetData(DestinationType type);

        // get tickets
    }
    
    public class FlightDataService : IFlightDataService
    {
        public string CreateSession(string intentJson)
        {
            return "";
        }

        public async Task<DestinationEntity[]> GetData(DestinationType type)
        {
            var url = "http://api.travelpayouts.com/data/ru/" + 
                (type == DestinationType.Country 
                    ? "countries.json" 
                    : "cities.json");

            var client = new HttpClient();
            var response = await client.GetAsync(url);

            if(response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DestinationEntity[]>(json);
            }
            else
            {
                return null;
            }
        }
    }
}
