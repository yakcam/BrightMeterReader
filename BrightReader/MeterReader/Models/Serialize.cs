using Newtonsoft.Json;

namespace MeterReader.Models
{
    internal static class Serialize
    {
        public static string ToJson(this AuthRequest self) => JsonConvert.SerializeObject(self, MeterReader.Models.Converter.Settings);

        public static string ToJson(this AuthResponse self) => JsonConvert.SerializeObject(self, MeterReader.Models.Converter.Settings);

    }
}
