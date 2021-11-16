using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using System;
using System.Configuration;
using EP.NETCore.BOT.Data;

namespace PVABotConnector
{
    class Program
    {
        private const string _botDisplayName = "Bot";
        private const string _userDisplayName = "You";
        private static string s_endConversationMessage = "Quit";
        private static IDistributedCache _cache;

        static void Main(string[] args)
        {
            TestChildBotHandler testChildBotHandler;
            string inputMessage;
            string SessionId = Guid.NewGuid().ToString();
            RedisCacheOptions _option = new RedisCacheOptions() { Configuration = Convert.ToString(ConfigurationManager.ConnectionStrings["CacheService"]) };
            _cache = new RedisCache(_option);
            while (!string.Equals(inputMessage = GetUserInput(), s_endConversationMessage, StringComparison.OrdinalIgnoreCase))
            {
                testChildBotHandler = new TestChildBotHandler(_cache, SessionId);
                var response = testChildBotHandler.GetBotResponse(inputMessage).GetAwaiter().GetResult();
                ShowResponse(response);
            }
        }

        private static string GetUserInput()
        {
            Console.WriteLine($"{_userDisplayName}:");
            var inputMessage = Console.ReadLine();
            return inputMessage;
        }

        private static void ShowResponse(UtteranceResponse response)
        {
            foreach(var message in response.Messages)
            {
                Console.WriteLine($"{_botDisplayName}:{message.Content}");
            }
        }
    }
}
