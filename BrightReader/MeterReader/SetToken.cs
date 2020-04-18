using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MeterReader.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Slack.Webhooks;

namespace MeterReader
{
    public static class SetToken
    {
        private static HttpClient _httpClient = new HttpClient();
        private static bool _httpClientSetupDone = false;

        private static string _webhookUrl = Environment.GetEnvironmentVariable("SlackWebhookUrl");
        private static string _keyVaultName = Environment.GetEnvironmentVariable("KeyVaultName");

        [FunctionName("SetToken")]
        public async static Task Run([TimerTrigger("29 0 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"SetToken function executed at: {DateTime.Now.ToLocalTime().ToString(Constants.DateTimeFormatString)}");
            var slackMessenger = new SlackMessenger(_webhookUrl);
            try
            {
                var currentAuthToken = await KeyVault.GetAuthTokenFromKeyVault(_keyVaultName);
                var currentAuthTokenExpiryDate = GetExpiryDate(currentAuthToken);

                if (currentAuthTokenExpiryDate < DateTimeOffset.Now.AddDays(2))
                {
                    log.LogInformation("Auth token expires soon, starting refresh process...");
                    
                    SetupHttpClient();

                    var username = Environment.GetEnvironmentVariable("BrightUsername");
                    var password = Environment.GetEnvironmentVariable("BrightPassword");
                    var authResponse = await GetAuthResponse(username, password);

                    if (authResponse.Valid)
                    {
                        log.LogInformation($"New auth token was retrieved successfully. Expiry date: {authResponse.ExpiryDate.ToLocalTime().ToString(Constants.DateTimeFormatString)}");
                        var secretVersion = await KeyVault.SaveAuthTokenToKeyVault(authResponse.Token, _keyVaultName);
                        log.LogInformation($"Auth token was saved to Azure Key Vault, secret version: '{secretVersion}'.");
                    }
                    else
                    {
                        throw new Exception("Auth token response was not valid.");
                    }
                
                }
                else
                {
                    log.LogInformation($"Auth token expires at {currentAuthTokenExpiryDate.ToLocalTime().ToString(Constants.DateTimeFormatString)}, not refreshing yet.");
                }

            }
            catch (Exception ex)
            {
                var error = $"Error in SetToken: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
                log.LogError(error);
                await slackMessenger.SendMessage(error, Emoji.Exclamation, "SetToken Error");
                throw;
            }
            log.LogInformation($"SetToken function completed at: {DateTime.Now.ToLocalTime().ToString(Constants.DateTimeFormatString)}");
        }

        private async static Task<AuthResponse> GetAuthResponse(string username, string password)
        {
            var requestModel = new AuthRequest
            {
                ApplicationId = Constants.BrightApplicationIdString,
                Username = username,
                Password = password
            };

            var response = await _httpClient.PostAsJsonAsync($"https://api.glowmarkt.com/api/v0-1/auth", requestModel);
            response.EnsureSuccessStatusCode();
            return AuthResponse.FromJson(await response.Content.ReadAsStringAsync());
        }

        private static DateTimeOffset GetExpiryDate(string authToken)
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(authToken);
            return new DateTimeOffset(jwt.ValidTo);
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
                _httpClientSetupDone = true;
            }
        }
    }
}
