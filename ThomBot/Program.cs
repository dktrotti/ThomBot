using Discord.WebSocket;
using Discord.Audio;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ThomBot
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        private static async Task MainAsync(string[] args)
        {
            string secretsFilePath;
            if (args.Length >= 1)
            {
                secretsFilePath = args[0];
            }
            else
            {
                secretsFilePath = "secrets.json";
            }

            dynamic secrets = JObject.Parse(File.ReadAllText(secretsFilePath));

            using var client = new DiscordSocketClient();
            await client.LoginAsync(Discord.TokenType.Bot, secrets.discordToken);
            await client.StartAsync();

            client.Ready += () => Client_Ready(client);

        }

        private static async Task Client_Ready(DiscordSocketClient client)
        {
            Console.WriteLine("Selecting guild...");
            var guild = PromptSelection(client.Guilds, g => g.Name);
            Console.WriteLine("Selecting voice channel...");
            var channel = PromptSelection(guild.VoiceChannels, ch => ch.Name);

            Console.WriteLine("Connecting to voice channel...");
            var audioClient = await channel.ConnectAsync();

            Console.WriteLine("Hello World!");

            throw new NotImplementedException();
        }

        private static T PromptSelection<T>(IEnumerable<T> choices, Func<T, string> ToString)
        {
            if (choices.Count() == 0)
            {
                throw new ArgumentException("Zero choices provided");
            }

            if (choices.Count() == 1)
            {
                return choices.First();
            }

            Console.WriteLine("Select one of the following:");
            choices
                .Select((t, i) => $"{i + 1}: {ToString(t)}")
                .ToList()
                .ForEach(Console.WriteLine);

            while(true)
            {
                var selectionStr = Console.ReadLine();
                int selection;
                if (int.TryParse(selectionStr, out selection) && 1 <= selection && selection <= choices.Count())
                {
                    return choices.ToList()[selection - 1];
                }
                else
                {
                    Console.WriteLine("Invalid selection, try again");
                    continue;
                }
            }
        }
    }
}
