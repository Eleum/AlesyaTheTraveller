export interface FlightData {
  ImageUri: string;
  Departure: Date;
  Arrival: Date;
  Origin: string;
  Destination: string;
  Cost: number;
  Stops: number;
  TicketSellerUri: string;
}
