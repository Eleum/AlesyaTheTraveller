using AlesyaTheTraveller.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Services
{
    public interface IFlightDataCacheService
    {
        bool AddData(string key, DestinationEntity value);
        DestinationEntity GetDestination(string stringLookUp);
        DestinationEntity GetCountryByCode(string code);
    }

    public class FlightDataCacheService : IFlightDataCacheService
    {
        private readonly ConcurrentDictionary<string, DestinationEntity> _cache;

        public FlightDataCacheService()
        {
            _cache = new ConcurrentDictionary<string, DestinationEntity>();
            Debug.WriteLine("singleton initiallized");
        }

        public DestinationEntity GetDestination(string stringLookUp)
        {
            var a = _cache.Where(x => x.Value.NameTranslations.En.ToLower() == stringLookUp.ToLower());
            return _cache
                .Where(x => x.Value.NameTranslations.En.ToLower() == stringLookUp.ToLower())
                .FirstOrDefault()
                .Value;
        }

        public DestinationEntity GetCountryByCode(string code)
        {
            return _cache.FirstOrDefault(x => x.Value.Code == code).Value;
        }

        public bool AddData(string key, DestinationEntity value)
        {
            return _cache.TryAdd<string, DestinationEntity>(key, value);
        }
    }
}
