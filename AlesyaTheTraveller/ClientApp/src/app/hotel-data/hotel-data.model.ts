export interface HotelData {
  Id: number;
  Country: string;
  City: string;
  OriginAddress: string;
  Address: string;
  OriginName: string;
  Name: string;
  ImageUri: string;
  Class: number;
  IsFreeCancellation: boolean;
  IsNoPrepayment: boolean;
  IsSoldOut: boolean;
  CurrencyCode: string;
  TotalPrice: number;
  ReviewScore: number;
  ReviewScoreWord: string;
  ReviewsCount: number;
}
