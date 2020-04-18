using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MeterReader.Models
{
    internal class MeterReadResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("resourceTypeId")]
        public Guid ResourceTypeId { get; set; }

        [JsonPropertyName("resourceId")]
        public Guid ResourceId { get; set; }

        [JsonPropertyName("data")]
        public List<List<Int64>> Data { get; set; }

        [JsonPropertyName("units")]
        public string Units { get; set; }

        [JsonPropertyName("classifier")]
        public string Classifier { get; set; }

        public Tuple<DateTimeOffset, Int64> GetReading()
        {
            return new Tuple<DateTimeOffset, long>(DateTimeOffset.FromUnixTimeSeconds(this.Data[0][0]), this.Data[0][1]);
        }
    }
}
