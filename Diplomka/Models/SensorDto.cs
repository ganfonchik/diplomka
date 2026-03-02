using System;
using System.Text.Json.Serialization;

namespace Diplomka.Models
{
    public class SensorDto
    {
        [JsonPropertyName("temp_lm35")]
        public double? TempLm35 { get; set; }

        [JsonPropertyName("temp_dht")]
        public double? TempDht { get; set; }

        [JsonPropertyName("humidity")]
        public double? Humidity { get; set; }

        [JsonPropertyName("light")]
        public double? Light { get; set; }

        [JsonPropertyName("co2")]
        public double? Co2 { get; set; }

        [JsonPropertyName("water")]
        public double? Water { get; set; }

        [JsonPropertyName("sound")]
        public double? Sound { get; set; }
    }
}
