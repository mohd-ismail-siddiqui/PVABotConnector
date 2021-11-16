using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Microsoft.Bot.Connector.DirectLine;
using EP.NETCore.BOT.Data;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading;

namespace PVABotConnector
{
    public class DirectLineToken
    {
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="token">Directline token string</param>
        public DirectLineToken(string token)
        {
            Token = token;
        }

        public string Token { get; set; }
    }

    public class TestChildBotHandler: IDisposable
    {

        private string _watermark = null;
        private const string _botName = "EchoBot";
        private readonly HttpClient _httpClient = new HttpClient();
        private IDistributedCache _cache;
        private DistributedCacheEntryOptions _cacheOptions;
        private DirectLineClient _directLineClient;
        private Conversation _conversation;
        private string _conversationId;
        private string _sessionId;
        private JsonSerializerSettings _deserializationSettings;
        private string botId = "";
        private string tenantId = "";

        public TestChildBotHandler(IDistributedCache cache, string sessionId)
        {
            _cache = cache;
            _sessionId = sessionId;
            _cacheOptions = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromHours(12));
        }

        private async Task<string> GetTokenAsync()
        {
            string token;
            using (var httpRequest = new HttpRequestMessage())
            {
                httpRequest.Method = HttpMethod.Get;
                UriBuilder uriBuilder = new UriBuilder("https://powerva.microsoft.com/api/botmanagement/v1/directline/directlinetoken?");
                uriBuilder.Query = $"botId={botId}&tenantId={tenantId}";
                httpRequest.RequestUri = uriBuilder.Uri;
                using (var response = await _httpClient.SendAsync(httpRequest))
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    token = JsonConvert.DeserializeObject<DirectLineToken>(responseString).Token;
                }
            }

            return token;
        }

        public async Task<UtteranceResponse> GetBotResponse(string inputMessage)
        {
            var token = await GetTokenAsync();
            UtteranceResponse responses = null;

            try
            {
                _directLineClient = new DirectLineClient(token);
                _directLineClient = JsonConvert.DeserializeObject<DirectLineClient>(await _cache.GetStringAsync($"{_sessionId}_DirectLineClient"), _directLineClient.DeserializationSettings);
                _conversation = JsonConvert.DeserializeObject<Conversation>(await _cache.GetStringAsync($"{_sessionId}_Conversation"));
                _conversationId = JsonConvert.DeserializeObject<string>(await _cache.GetStringAsync($"{_sessionId}_ConversationId"));
            }
            catch(Exception ex)
            {
                _directLineClient = null;
                _conversation = null;
            }
            
            if (_directLineClient == null)
                _directLineClient = new DirectLineClient(token);

            if (_conversation == null)
            {
                _conversation = await _directLineClient.Conversations.StartConversationAsync();
                _conversationId = _conversation.ConversationId;
            }

            if (inputMessage.Equals("Open Child Bot", StringComparison.OrdinalIgnoreCase))
                inputMessage = "Hi";

            await _directLineClient.Conversations.PostActivityAsync(_conversationId, new Activity()
            {
                Type = ActivityTypes.Message,
                From = new ChannelAccount { Id = "userId", Name = "userName" },
                Text = inputMessage,
                TextFormat = "plain",
                Locale = "en-Us",
            });


            Thread.Sleep(3000);
            // Get bot response using directlinClient
            responses = BotReply(await GetBotResponseActivitiesAsync(_directLineClient, _conversationId));

            var _directLineClientJSON = JsonConvert.SerializeObject(_directLineClient, Formatting.None,
                        _directLineClient.SerializationSettings);


            await _cache.SetStringAsync($"{_sessionId}_DirectLineClient", _directLineClientJSON, _cacheOptions);
            await _cache.SetStringAsync($"{_sessionId}_Conversation", JsonConvert.SerializeObject(_conversation), _cacheOptions);
            await _cache.SetStringAsync($"{_sessionId}_ConversationId", JsonConvert.SerializeObject(_conversationId), _cacheOptions);

            return responses;
        }

        /// <summary>
        /// Use directlineClient to get bot response
        /// </summary>
        /// <returns>List of DirectLine activities</returns>
        /// <param name="directLineClient">directline client</param>
        /// <param name="conversationtId">current conversation ID</param>
        /// <param name="botName">name of bot to connect to</param>
        private async Task<List<Activity>> GetBotResponseActivitiesAsync(DirectLineClient directLineClient, string conversationtId)
        {
            ActivitySet response = null;
            List<Activity> result = new List<Activity>();

            do
            {
                response = await directLineClient.Conversations.GetActivitiesAsync(conversationtId, _watermark);
                if (response == null)
                {
                    // response can be null if directLineClient token expires
                    //Console.WriteLine("Conversation expired. Press any key to exit.");
                    //Console.Read();
                    //directLineClient.Dispose();
                    //Environment.Exit(0);
                }

                _watermark = response?.Watermark;
                result = response?.Activities?.Where(x =>
                  x.Type == ActivityTypes.Message &&
                    string.Equals(x.From.Name, _botName, StringComparison.Ordinal)).ToList();

                if (result != null && result.Any())
                {
                    return result;
                }
                Thread.Sleep(1000);

            } while (response != null && response.Activities.Any());

            return new List<Activity>();
        }

        /// <summary>
        /// Print bot reply to console
        /// </summary>
        /// <param name="responses">List of DirectLine activities <see cref="https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md"/>
        /// </param>
        private UtteranceResponse BotReply(List<Activity> responses)
        {
            UtteranceResponse response = new UtteranceResponse() { EndConversation = false, Messages = new List<UtteranceResMessage>() };


            responses?.ForEach(responseActivity =>
            {
                // responseActivity is standard Microsoft.Bot.Connector.DirectLine.Activity
                // See https://github.com/Microsoft/botframework-sdk/blob/master/specs/botframework-activity/botframework-activity.md for reference
                // Showing examples of Text & SuggestedActions in response payload
                if (!string.IsNullOrEmpty(responseActivity.Text))
                {
                    response.Messages.Add(new UtteranceResMessage { Content = responseActivity.Text, IsCardContent = false, Sequence = 1, VoiceContent = responseActivity.Text, AltContent = responseActivity.Text });
                }

                if (responseActivity.SuggestedActions != null && responseActivity.SuggestedActions.Actions != null)
                {
                    var options = responseActivity.SuggestedActions?.Actions?.Select(a => a.Title).ToList();
                    Console.WriteLine($"\t{string.Join(" | ", options)}");
                }
            });

            return response;
        }

        public void Dispose()
        {
        }
    }
}
