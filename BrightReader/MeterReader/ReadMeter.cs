using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Slack.Webhooks;

namespace MeterReader
{
    public static class ReadMeter
    {
        private static HttpClient _httpClient = new HttpClient();
        private static bool _httpClientSetupDone = false;

        private static string _webhookUrl = System.Environment.GetEnvironmentVariable("SlackWebhookUrl");
        private static string _gasResourceId = System.Environment.GetEnvironmentVariable("GasResourceId");
        private static string _electricResourceId = System.Environment.GetEnvironmentVariable("ElectricResourceId");
        private static string _authToken = System.Environment.GetEnvironmentVariable("BrightAuthToken");

        private static readonly decimal _calorificValue = 39.2M;
        private static readonly decimal _correction = 1.02264M;

        [FunctionName("ReadMeter")]
        public async static Task Run([TimerTrigger("0 0 4 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"ReadMeter function executed at: {DateTime.Now}");

            try
            {
                SetupHttpClient();

                var gasReadingResponse = await GetReading(_gasResourceId);
                var gasReading = gasReadingResponse.GetReading();

                var electricReadingResponse = await GetReading(_electricResourceId);
                var electricReading = electricReadingResponse.GetReading();

                await SendMessage($"Electric: *{electricReading.Item2 / 1000}* kWh. Date {electricReading.Item1.ToLocalTime().ToString()}", Emoji.Zap, "Electric");

                var gasVolume = (3.6M * (((decimal)gasReading.Item2) / (decimal)1000)) / (_calorificValue * _correction);
                await SendMessage($"Gas: *{Math.Round(gasVolume)}* m³. Date {gasReading.Item1.ToLocalTime().ToString()}", Emoji.Fire, "Gas");
            }
            catch (Exception ex)
            {
                var error = $"Error in ReadMeter: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                log.LogError(error);
                await SendMessage(error, Emoji.Exclamation, "Error");
                throw;
            }

            log.LogInformation($"ReadMeter function completed at: {DateTime.Now}");
        }

        private static async Task SendMessage(string message, string icon, string username)
        {
            var slackClient = new SlackClient(_webhookUrl);
            await slackClient.PostAsync(new SlackMessage
            {
                Text = message,
                IconEmoji = icon,
                Username = username
            });
        }

        private static async Task<MeterReadResponse> GetReading(string resourceId)
        {
            var response = await _httpClient.GetAsync($"https://api.glowmarkt.com/api/v0-1/resource/{resourceId}/meterread");
            response.EnsureSuccessStatusCode();
            using (var contentStream = response.Content.ReadAsStreamAsync())
            { 
                return await JsonSerializer.DeserializeAsync<MeterReadResponse>(await contentStream);
            }
        }

        private static void SetupHttpClient()
        {
            if (!_httpClientSetupDone)
            {
                _httpClient.DefaultRequestHeaders.Accept.Clear();
                _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "yakcamBrightReader/0.1.0");
                _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
                _httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                _httpClient.DefaultRequestHeaders.Add("Connection", "keep-alive");
                // Token
                _httpClient.DefaultRequestHeaders.Add("token", _authToken);
                _httpClient.DefaultRequestHeaders.Add("applicationId", "b0f1b774-a586-4f72-9edd-27ead8aa7a8d");
                _httpClientSetupDone = true;
            }
        }

        private class MeterReadResponse
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
}
