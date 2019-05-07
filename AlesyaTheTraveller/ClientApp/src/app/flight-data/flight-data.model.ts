export interface Query {
  Country: string;
  Currency: string;
  Locale: string;
  Adults: number;
  Children: number;
  Infants: number;
  OriginPlace: string;
  DestinationPlace: string;
  OutboundDate: string;
  LocationSchema: string;
  CabinClass: string;
  GroupPricing: boolean;
}

export interface PricingOption {
  Agents: number[];
  QuoteAgeInMinutes: number;
  Price: number;
  DeeplinkUrl: string;
}

export interface BookingDetailsLink {
  Uri: string;
  Body: string;
  Method: string;
}

export interface Itinerary {
  OutboundLegId: string;
  PricingOptions: PricingOption[];
  BookingDetailsLink: BookingDetailsLink;
}

export interface FlightNumber {
  FlightNumber: string;
  CarrierId: number;
}

export interface Leg {
  Id: string;
  SegmentIds: number[];
  OriginStation: number;
  DestinationStation: number;
  Departure: Date;
  Arrival: Date;
  Duration: number;
  JourneyMode: string;
  Stops: number[];
  Carriers: number[];
  OperatingCarriers: number[];
  Directionality: string;
  FlightNumbers: FlightNumber[];
}

export interface Segment {
  Id: number;
  OriginStation: number;
  DestinationStation: number;
  DepartureDateTime: Date;
  ArrivalDateTime: Date;
  Carrier: number;
  OperatingCarrier: number;
  Duration: number;
  FlightNumber: string;
  JourneyMode: string;
  Directionality: string;
}

export interface Carrier {
  Id: number;
  Code: string;
  Name: string;
  ImageUrl: string;
  DisplayCode: string;
}

export interface Agent {
  Id: number;
  Name: string;
  ImageUrl: string;
  Status: string;
  OptimisedForMobile: boolean;
  Type: string;
}

export interface Place {
  Id: number;
  Code: string;
  Type: string;
  Name: string;
  ParentId?: number;
}

export interface Currency {
  Code: string;
  Symbol: string;
  ThousandsSeparator: string;
  DecimalSeparator: string;
  SymbolOnLeft: boolean;
  SpaceBetweenAmountAndSymbol: boolean;
  RoundingCoefficient: number;
  DecimalDigits: number;
}

export interface RootObject {
  SessionKey: string;
  Query: Query;
  Status: string;
  Itineraries: Itinerary[];
  Legs: Leg[];
  Segments: Segment[];
  Carriers: Carrier[];
  Agents: Agent[];
  Places: Place[];
  Currencies: Currency[];
}

export interface FlightData {
  ImageUri: string;
  Departure: Date;
  Arrival: Date;
  Origin: string;
  Destination: string;
  Cost: number;
  Type: string;
  Stops: number;
}
