using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MeterReader.Models;
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

        private static string _keyVaultName = Environment.GetEnvironmentVariable("KeyVaultName");
        private static string _webhookUrl = System.Environment.GetEnvironmentVariable("SlackWebhookUrl");
        private static string _gasResourceId = System.Environment.GetEnvironmentVariable("GasResourceId");
        private static string _electricResourceId = System.Environment.GetEnvironmentVariable("ElectricResourceId");

        private static readonly decimal _calorificValue = 39.2M;
        private static readonly decimal _correction = 1.02264M;

        [FunctionName("ReadMeter")]
        public async static Task Run([TimerTrigger("0 0 4 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"ReadMeter function executed at: {DateTime.Now.ToLocalTime().ToString(Constants.DateTimeFormatString)}");

            var slackMessenger = new SlackMessenger(_webhookUrl);

            try
            {
                var brightAuthToken = await KeyVault.GetAuthTokenFromKeyVault(_keyVaultName);
                SetupHttpClient(brightAuthToken);

                var gasReadingResponse = await GetReading(_gasResourceId);
                var gasReading = gasReadingResponse.GetReading();

                var electricReadingResponse = await GetReading(_electricResourceId);
                var electricReading = electricReadingResponse.GetReading();

                await slackMessenger.SendMessage($"Electric: *{electricReading.Item2 / 1000}* kWh. Date {electricReading.Item1.ToLocalTime().ToString(Constants.DateTimeFormatString)}", Emoji.Zap, "Electric");

                var gasVolume = 3.6M * ((gasReading.Item2) / 1000M) / (_calorificValue * _correction);
                await slackMessenger.SendMessage($"Gas: *{Math.Round(gasVolume)}* m³. Date {gasReading.Item1.ToLocalTime().ToString(Constants.DateTimeFormatString)}", Emoji.Fire, "Gas");
            }
            catch (Exception ex)
            {
                var error = $"Error in ReadMeter: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                log.LogError(error);
                await slackMessenger.SendMessage(error, Emoji.Exclamation, "ReadMeter Error");
                throw;
            }

            log.LogInformation($"ReadMeter function completed at: {DateTime.Now.ToLocalTime().ToString(Constants.DateTimeFormatString)}");
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

        private static void SetupHttpClient(string authToken)
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
                _httpClient.DefaultRequestHeaders.Add("token", authToken);
                _httpClient.DefaultRequestHeaders.Add("applicationId", Constants.BrightApplicationIdString);
                _httpClientSetupDone = true;
            }
        }
    }
}
