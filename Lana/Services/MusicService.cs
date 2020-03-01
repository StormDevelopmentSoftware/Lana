using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Lana.Entities.Music;

namespace Lana.Services
{
	[DebuggerDisplay("Players: {Players.Count}")]
	public class MusicService
	{
		protected Timer NodeTimer;
		protected ConcurrentDictionary<ulong, MusicPlayer> Players { get; set; }
		public LavalinkNodeConnection Node { get; private set; }
		protected LavalinkExtension Lavalink { get; set; }
		protected DiscordClient Discord { get; set; }
		protected LanaBot Bot { get; set; }
		protected SemaphoreSlim Sem = new SemaphoreSlim(1, 1);

		public MusicService(LanaBot bot)
		{
			this.Bot = bot;
			this.Discord = this.Bot.Discord;
			this.Lavalink = this.Bot.Lavalink;
			this.Players = new ConcurrentDictionary<ulong, MusicPlayer>();
		}

		public Task<LavalinkLoadResult> GetTracksAsync(string query)
			=> this.Node?.Rest?.GetTracksAsync(query, LavalinkSearchType.Youtube);

		public Task<LavalinkLoadResult> GetTracksAsync(Uri url)
			=> this.Node?.Rest?.GetTracksAsync(url);

		public Task<MusicPlayer> GetOrCreateAsync(DiscordGuild guild)
		{
			if (this.Players.TryGetValue(guild.Id, out var player))
				return Task.FromResult(player);

			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.Write("[LanaBot/MusicService] ");
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("Criando um novo player para a guild {0} ({1})", guild.Name, guild.Id);

			player = new MusicPlayer(guild, this.Node);
			this.Players.AddOrUpdate(guild.Id, player, (key, old) => player);
			return Task.FromResult(player);
		}

		public Task InitializeAsync()
		{
			this.Discord.Ready += this.NotifyReady;
			this.NodeTimer = new Timer(NotifyTimerTick);
			return Task.CompletedTask;
		}

		protected Task NotifyReady(ReadyEventArgs e)
		{
			this.NodeTimer.Change(TimeSpan.FromSeconds(15));
			return Task.CompletedTask;
		}

		void NotifyTimerTick(object state)
			=> this.UpdateNodeConnectionAsync().GetAwaiter().GetResult(); // forçar o timer ser bloqueado enquanto estiver atualizando.

		async Task UpdateNodeConnectionAsync()
		{
			await this.Sem.WaitAsync();

			try
			{
				if (this.Node == null)
					this.Node = this.Lavalink.GetNodeConnection(this.Bot.Configuration.Lavalink.SocketEndpoint);

				if (this.Node == null || !this.Node.IsConnected)
				{
					this.Node = await this.Lavalink.ConnectAsync(this.Bot.Configuration.Lavalink.Build());
					this.Node.Disconnected += this.NotifyNodeDisconnected;
				}

				if (this.Node == null || !this.Node.IsConnected)
				{
					Console.ForegroundColor = ConsoleColor.Blue;
					Console.Write("[LanaBot/Lavalink] ");
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Não foi possivel estabelecer conexao com o lavalink!");

					var tasks = new List<Task>();

					foreach (var player in this.Players.Select(x => x.Value))
						tasks.Add(player.ShutdownAsync());

					await Task.WhenAll(tasks);
				}
				else
				{
					Console.ForegroundColor = ConsoleColor.Blue;
					Console.Write("[LanaBot/Lavalink] ");
					Console.ForegroundColor = ConsoleColor.Gray;
					Console.WriteLine("Conexão com lavalink estável.");
					this.NodeTimer.Change(TimeSpan.FromMinutes(3)); // Conexão estável, não precisa atualizar rápido de mais.
				}
			}
			catch(Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Blue;
				Console.Write("[LanaBot/Lavalink] ");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("Falha ao conectar-se ao nodo do lavalink!\n{0}", ex);
				this.NodeTimer.Change(TimeSpan.FromSeconds(5));
			}
			finally
			{
				this.Sem.Release();
			}
		}

		protected Task NotifyNodeDisconnected(NodeDisconnectedEventArgs e)
		{
			if(this.Node == null)
				this.Node.Disconnected -= this.NotifyNodeDisconnected;

			this.NodeTimer.Change(TimeSpan.FromSeconds(15));
			return Task.CompletedTask;
		}
	}
}
