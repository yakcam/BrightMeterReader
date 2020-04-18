using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using MeterReader.Models;
using System;
using System.Threading.Tasks;

namespace MeterReader
{
    internal static class KeyVault
    {
        public async static Task<string> GetAuthTokenFromKeyVault(string keyVaultName)
        {
            var client = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net"), new DefaultAzureCredential());
            var response = await client.GetSecretAsync(Constants.BrightAuthTokenSecretName);
            return response.Value.Value;
        }

        public async static Task<string> SaveAuthTokenToKeyVault(string token, string keyVaultName)
        {
            var client = new SecretClient(new Uri($"https://{keyVaultName}.vault.azure.net"), new DefaultAzureCredential());
            var response = await client.SetSecretAsync(Constants.BrightAuthTokenSecretName, token);
            return response.Value.Properties.Version;
        }
    }
}
