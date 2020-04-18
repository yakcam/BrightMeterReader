using System;
using Newtonsoft.Json;

namespace MeterReader.Models
{
    internal partial class AuthResponse
    {
        [JsonProperty("valid")]
        public bool Valid { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("exp")]
        public long Exp { get; set; }

        [JsonProperty("userGroups")]
        public object[] UserGroups { get; set; }

        [JsonProperty("functionalGroupAccounts")]
        public object[] FunctionalGroupAccounts { get; set; }

        [JsonProperty("accountId")]
        public Guid AccountId { get; set; }

        [JsonProperty("isTempAuth")]
        public bool IsTempAuth { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }

    internal partial class AuthResponse
    {
        public DateTimeOffset ExpiryDate => DateTimeOffset.FromUnixTimeSeconds(this.Exp);
    }

    internal partial class AuthResponse
    {
        public static AuthResponse FromJson(string json) => JsonConvert.DeserializeObject<AuthResponse>(json, MeterReader.Models.Converter.Settings);
    }
}