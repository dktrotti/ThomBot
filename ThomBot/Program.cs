using Discord.WebSocket;
using Discord.Audio;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Speech.AudioFormat;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;
using Discord;

namespace ThomBot
{
    /// <summary>
    /// All of this is terrible code with no intention of being maintained.
    /// </summary>
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

            var definition = new { DiscordToken = "" };
            var secrets = JsonConvert.DeserializeAnonymousType(File.ReadAllText(secretsFilePath), definition);

            using var client = new DiscordSocketClient();
            await client.LoginAsync(Discord.TokenType.Bot, secrets.DiscordToken);
            await client.StartAsync();

            client.Ready += () => Client_Ready(client);

            await Task.Delay(-1);
        }

        private static async Task Client_Ready(DiscordSocketClient client)
        {
            try
            {

                Console.WriteLine("Selecting guild...");
                var guild = PromptSelection(client.Guilds, g => g.Name);
                Console.WriteLine("Selecting voice channel...");
                var channel = PromptSelection(guild.VoiceChannels, ch => ch.Name);

                Console.WriteLine("Setting nickname...");
                await guild.CurrentUser.ModifyAsync(u => {
                    u.Nickname = "groupsex fo telmaH";
                });

                Console.WriteLine("Connecting to voice channel...");
                var audioClient = await channel.ConnectAsync();

                using var synthesizer = new SpeechSynthesizer();
                using var discordStream = audioClient.CreatePCMStream(AudioApplication.Voice);

                var voice = PromptSelection(synthesizer.GetInstalledVoices(), v => v.VoiceInfo.Name);
                synthesizer.SelectVoice(voice.VoiceInfo.Name);

                while (true)
                {
                    synthesizer.SetOutputToWaveFile("temp.wav");

                    Console.WriteLine("What should Thomas say?");
                    var response = Console.ReadLine();
                    synthesizer.Speak(new Prompt(response));

                    {
                        // From: https://docs.stillu.cc/guides/voice/sending-voice.html
                        using var ffmpeg = CreateStream("temp.wav");
                        using var output = ffmpeg.StandardOutput.BaseStream;

                        try { await output.CopyToAsync(discordStream); }
                        finally { await discordStream.FlushAsync(); }
                    }

                    synthesizer.SetOutputToNull();
                    File.Delete("temp.wav");
                }
            }
            catch (Exception e)
            {
                // Exceptions are swallowed unless caught here
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);

                throw;
            }
        }

        // From: https://docs.stillu.cc/guides/voice/sending-voice.html
        private static Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                LoadUserProfile = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
            });
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
