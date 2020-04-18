using Slack.Webhooks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MeterReader
{
    public class SlackMessenger
    {
        private readonly string _webhookUrl;

        public SlackMessenger(string webhookUrl)
        {
            _webhookUrl = webhookUrl;
        }

        public async Task SendMessage(string message, string icon, string username)
        {
            var slackClient = new SlackClient(_webhookUrl);
            await slackClient.PostAsync(new SlackMessage
            {
                Text = message,
                IconEmoji = icon,
                Username = username
            });
        }
    }
}
