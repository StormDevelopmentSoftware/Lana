using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Lavalink;
using Lana.Entities;
using Lana.Services;
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
				.AddSingleton(new LanaEvents(this))
				.AddSingleton(new MusicService(this))
				.BuildServiceProvider(true);

			this.CommandsNext = this.Discord.UseCommandsNext(new CommandsNextConfiguration
			{
				StringPrefixes = ImmutableArray.Create("-", "l!"),
				EnableDms = false,
				Services = this.Services
			});

			this.CommandsNext.SetHelpFormatter<LanaHelpFormatter>();
			this.CommandsNext.RegisterCommands(typeof(LanaBot).Assembly);
		}

		public async Task InitializeAsync()
		{
			await this.Services.GetService<LanaEvents>().InitializeAsync();
			await this.Services.GetService<MusicService>().InitializeAsync();
			await this.Discord.ConnectAsync();
		}

		public Task ShutdownAsync()
			=> this.Discord.DisconnectAsync();
	}
}
