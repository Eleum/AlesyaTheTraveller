using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Entities
{
    public sealed class FlightData
    {
        public Uri CarrierImageUri { get; set; }

        public DateTimeOffset DepartureTime { get; set; }

        public DateTimeOffset ArrivalTime { get; set; }

        public string Origin { get; set; }

        public string Destination { get; set; }

        public double Cost { get; set; }

        public int Stops { get; set; }

        public Uri TicketSellerUri { get; set; }
    }
}