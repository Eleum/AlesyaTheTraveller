using AlesyaTheTraveller.Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Services
{
    public interface IFlightDataCacheService
    {
        bool AddData(string key, DestinationEntity value);
        DestinationEntity GetDestination(string key);
    }

    public class FlightDataCacheService : IFlightDataCacheService
    {
        private readonly ConcurrentDictionary<string, DestinationEntity> _cache;

        public FlightDataCacheService()
        {
            _cache = new ConcurrentDictionary<string, DestinationEntity>();
        }

        public DestinationEntity GetDestination(string key)
        {
            return _cache.GetValueOrDefault(key);
        }

        public bool AddData(string key, DestinationEntity value)
        {
            return _cache.TryAdd<string, DestinationEntity>(key, value);
        }
    }
}
