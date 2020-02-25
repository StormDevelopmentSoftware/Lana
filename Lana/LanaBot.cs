using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Lavalink;
using Lana.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Lana
{
    public class LanaBot
    {
        public static LanaBot Instance { get; private set; }
        public LanaConfiguration Configuration { get; private set; }
        public DiscordClient Discord { get; private set; }
        public CommandsNextExtension CommandsNext { get; private set; }
        public InteractivityExtension Interactivity { get; private set; }
        public LavalinkExtension Lavalink { get; private set; }
        public IServiceProvider Services { get; private set; }

        public LanaBot(LanaConfiguration config)
        {
            Instance = this;

            this.Configuration = config;
            this.Discord = new DiscordClient(this.Configuration.Discord.Build());
            this.Lavalink = this.Discord.UseLavalink();

            this.Interactivity = this.Discord.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehaviour = PaginationBehaviour.WrapAround,
                PaginationDeletion = PaginationDeletion.DeleteMessage,
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromMinutes(5d)
            });

            this.Services = new ServiceCollection()
                .AddSingleton(this)
                .BuildServiceProvider(true);

            this.CommandsNext = this.Discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes= ImmutableArray.Create("-", "l!"),
                EnableDms = false,
                Services = this.Services
            });

            this.CommandsNext.RegisterCommands(typeof(LanaBot).Assembly);
            this.CommandsNext.CommandErrored += ProcessCommandFailed;
        }

        Task ProcessCommandFailed(CommandErrorEventArgs e)
        {
            Console.WriteLine(e.Exception +"\n");
            return Task.CompletedTask;
        }

        public Task InitializeAsync()
            => this.Discord.ConnectAsync();

        public Task ShutdownAsync()
            => this.Discord.DisconnectAsync();
    }
}
