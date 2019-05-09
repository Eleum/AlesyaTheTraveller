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
        bool AddData(string key, GlobalPointEntity value);
        GlobalPointEntity GetLocation(string stringLookUp);
        GlobalPointEntity GetCountryByCode(string code);
    }

    public class FlightDataCacheService : IFlightDataCacheService
    {
        private readonly ConcurrentDictionary<string, GlobalPointEntity> _cache;

        public FlightDataCacheService()
        {
            _cache = new ConcurrentDictionary<string, GlobalPointEntity>();
        }

        public GlobalPointEntity GetLocation(string query)
        {
            var a = _cache.Where(x => x.Value.NameTranslations.En.ToLower() == query.ToLower());
            return _cache
                .Where(x => x.Value.NameTranslations.En.ToLower() == query.ToLower())
                .FirstOrDefault()
                .Value;
        }

        public GlobalPointEntity GetCountryByCode(string code)
        {
            return _cache.FirstOrDefault(x => x.Value.Code == code).Value;
        }

        public bool AddData(string key, GlobalPointEntity value)
        {
            return _cache.TryAdd<string, GlobalPointEntity>(key, value);
        }
    }
}
