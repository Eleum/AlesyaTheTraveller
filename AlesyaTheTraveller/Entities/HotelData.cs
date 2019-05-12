using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlesyaTheTraveller.Entities
{
    public class RootHotelObject
    {
        [JsonProperty("result")]
        public HotelData[] HotelData { get; set; }
    }

    public class HotelData
    {
        [JsonProperty("hotel_id")]
        public int Id { get; set; }

        [JsonProperty("country_trans")]
        public string Country { get; set; }

        [JsonProperty("city_trans")]
        public string City { get; set; }

        [JsonProperty("address")]
        public string OriginAddress { get; set; }

        [JsonProperty("address_trans")]
        public string Address { get; set; }

        [JsonProperty("hotel_name")]
        public string OriginName { get; set; }

        [JsonProperty("hotel_name_trans")]
        public string Name { get; set; }

        [JsonProperty("main_photo_url")]
        public Uri Image { get; set; }

        [JsonProperty("class")]
        public double Class { get; set; }

        [JsonProperty("is_free_cancellable")]
        public bool IsFreeCancellation { get; set; }

        [JsonProperty("is_no_prepayment_block")]
        public bool IsNoPrepayment { get; set; }

        [JsonProperty("soldout")]
        public bool IsSoldOut { get; set; }

        [JsonProperty("currencycode")]
        public string Currency { get; set; }

        [JsonProperty("min_total_price")]
        public double TotalPrice { get; set; }

        [JsonProperty("review_score")]
        public double ReviewScore { get; set; }

        [JsonProperty("review_score_word")]
        public string ReviewScoreWork { get; set; }

        [JsonProperty("review_nr")]
        public int ReviewsCount { get; set; }

        [JsonProperty("main_photo_id")]
        public int MainPhotoId { get; set; }
    }
}
