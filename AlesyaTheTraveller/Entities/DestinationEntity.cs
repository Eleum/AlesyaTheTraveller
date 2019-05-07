using System;
using Newtonsoft.Json;

namespace AlesyaTheTraveller.Entities
{
    #region Cache data

    public enum DestinationType
    {
        Country,
        City
    }

    public class DestinationEntity
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("name_translations")]
        public NameTranslations NameTranslations { get; set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; set; }
    }

    public class NameTranslations
    {
        [JsonProperty("en")]
        public string En { get; set; }
    }

    #endregion

    #region Skyscanner places list

    public class Places
    {
        [JsonProperty("Places")]
        public PlaceEntity[] Entities { get; set; }
    }

    public class PlaceEntity
    {
        [JsonProperty("PlaceId")]
        public string PlaceId { get; set; }

        [JsonProperty("PlaceName")]
        public string PlaceName { get; set; }

        [JsonProperty("CountryName")]
        public string CountryName { get; set; }
    }

    #endregion

    #region Skyscanner search result

    public class Query
    {
        [JsonProperty("Country")]
        public string Country { get; set; }

        [JsonProperty("Currency")]
        public string Currency { get; set; }

        [JsonProperty("Locale")]
        public string Locale { get; set; }

        [JsonProperty("Adults")]
        public long Adults { get; set; }

        [JsonProperty("Children")]
        public long Children { get; set; }

        [JsonProperty("Infants")]
        public long Infants { get; set; }

        [JsonProperty("OriginPlace")]
        public long OriginPlace { get; set; }

        [JsonProperty("DestinationPlace")]
        public long DestinationPlace { get; set; }

        [JsonProperty("OutboundDate")]
        public DateTimeOffset OutboundDate { get; set; }

        [JsonProperty("LocationSchema")]
        public string LocationSchema { get; set; }

        [JsonProperty("CabinClass")]
        public string CabinClass { get; set; }

        [JsonProperty("GroupPricing")]
        public bool GroupPricing { get; set; }
    }

    public class PricingOption
    {
        [JsonProperty("Agents")]
        public long[] Agents { get; set; }

        [JsonProperty("QuoteAgeInMinutes")]
        public long QuoteAgeInMinutes { get; set; }

        [JsonProperty("Price")]
        public double Price { get; set; }

        [JsonProperty("DeeplinkUrl")]
        public Uri DeeplinkUrl { get; set; }
    }

    public class BookingDetailsLink
    {
        [JsonProperty("Uri")]
        public string Uri { get; set; }

        [JsonProperty("Body")]
        public string Body { get; set; }

        [JsonProperty("Method")]
        public string Method { get; set; }
    }

    public class Itinerary
    {
        [JsonProperty("OutboundLegId")]
        public string OutboundLegId { get; set; }

        [JsonProperty("PricingOptions")]
        public PricingOption[] PricingOptions { get; set; }

        [JsonProperty("BookingDetailsLink")]
        public BookingDetailsLink BookingDetailsLink { get; set; }
    }

    public class FlightNumber
    {
        [JsonProperty("FlightNumber")]
        public long Number { get; set; }

        [JsonProperty("CarrierId")]
        public long CarrierId { get; set; }
    }

    public class Leg
    {
        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("SegmentIds")]
        public long[] SegmentIds { get; set; }

        [JsonProperty("OriginStation")]
        public long OriginStation { get; set; }

        [JsonProperty("DestinationStation")]
        public long DestinationStation { get; set; }

        [JsonProperty("Departure")]
        public DateTimeOffset Departure { get; set; }

        [JsonProperty("Arrival")]
        public DateTimeOffset Arrival { get; set; }

        [JsonProperty("Duration")]
        public long Duration { get; set; }

        [JsonProperty("JourneyMode")]
        public string JourneyMode { get; set; }

        [JsonProperty("Stops")]
        public long[] Stops { get; set; }

        [JsonProperty("Carriers")]
        public long[] Carriers { get; set; }

        [JsonProperty("OperatingCarriers")]
        public long[] OperatingCarriers { get; set; }

        [JsonProperty("Directionality")]
        public string Directionality { get; set; }

        [JsonProperty("FlightNumbers")]
        public FlightNumber[] FlightNumbers { get; set; }
    }

    public class Segment
    {
        [JsonProperty("Id")]
        public long Id { get; set; }

        [JsonProperty("OriginStation")]
        public long OriginStation { get; set; }

        [JsonProperty("DestinationStation")]
        public long DestinationStation { get; set; }

        [JsonProperty("DepartureDateTime")]
        public DateTimeOffset DepartureDateTime { get; set; }

        [JsonProperty("ArrivalDateTime")]
        public DateTimeOffset ArrivalDateTime { get; set; }

        [JsonProperty("Carrier")]
        public long Carrier { get; set; }

        [JsonProperty("OperatingCarrier")]
        public long OperatingCarrier { get; set; }

        [JsonProperty("Duration")]
        public long Duration { get; set; }

        [JsonProperty("FlightNumber")]
        public long FlightNumber { get; set; }

        [JsonProperty("JourneyMode")]
        public string JourneyMode { get; set; }

        [JsonProperty("Directionality")]
        public string Directionality { get; set; }
    }

    public class Carrier
    {
        [JsonProperty("Id")]
        public long Id { get; set; }

        [JsonProperty("Code")]
        public string Code { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("ImageUrl")]
        public Uri ImageUrl { get; set; }

        [JsonProperty("DisplayCode")]
        public string DisplayCode { get; set; }
    }

    public class Agent
    {
        [JsonProperty("Id")]
        public long Id { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("ImageUrl")]
        public Uri ImageUrl { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("OptimisedForMobile")]
        public bool OptimisedForMobile { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }
    }

    public class Place
    {
        [JsonProperty("Id")]
        public long Id { get; set; }

        [JsonProperty("Code")]
        public string Code { get; set; }

        [JsonProperty("Type")]
        public string Type { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("ParentId", NullValueHandling = NullValueHandling.Ignore)]
        public long? ParentId { get; set; }
    }

    public class Currency
    {
        public string Code { get; set; }
        public string Symbol { get; set; }
        public string ThousandsSeparator { get; set; }
        public string DecimalSeparator { get; set; }
        public bool SymbolOnLeft { get; set; }
        public bool SpaceBetweenAmountAndSymbol { get; set; }
        public int RoundingCoefficient { get; set; }
        public int DecimalDigits { get; set; }
    }

    public class RootObject
    {
        [JsonProperty("SessionKey")]
        public Guid SessionKey { get; set; }

        [JsonProperty("Query")]
        public Query Query { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }

        [JsonProperty("Itineraries")]
        public Itinerary[] Itineraries { get; set; }

        [JsonProperty("Legs")]
        public Leg[] Legs { get; set; }

        [JsonProperty("Segments")]
        public Segment[] Segments { get; set; }

        [JsonProperty("Carriers")]
        public Carrier[] Carriers { get; set; }

        [JsonProperty("Agents")]
        public Agent[] Agents { get; set; }

        [JsonProperty("Places")]
        public Place[] Places { get; set; }

        [JsonProperty("Currencies")]
        public Currency[] Currencies { get; set; }
    }

    #endregion
}