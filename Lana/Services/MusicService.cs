using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.Entities;
using DSharpPlus.Lavalink.EventArgs;
using Lana.Entities.Lavalink;
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
			this.NodeTimer = new Timer(NotifyTimerTick);
			this.NodeTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(15));
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
					this.NodeTimer.Change(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60)); // Conexão estável, não precisa atualizar rápido de mais.
				}
			}
			catch(Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Blue;
				Console.Write("[LanaBot/Lavalink] ");
				Console.ForegroundColor = ConsoleColor.Yellow;
				Console.WriteLine("Falha ao conectar-se ao nodo do lavalink!\n{0}", ex);
				this.NodeTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
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

			this.NodeTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(15));
			return Task.CompletedTask;
		}
	}
}

namespace Lana.Entities.Music
{
	public delegate Task NowPlayingEventHandler(TrackInfo track);

	[DebuggerDisplay("Connected: {IsConnected}, Tracks: {Count}")]
	public class MusicPlayer
	{
		public static readonly IComparer<TrackInfo> Shuffler;

		public bool IsConnected => this.Connection?.IsConnected == true;
		public NowPlayingEventHandler NowPlayingObserver;

		public DiscordGuild Guild { get; private set; }
		protected List<TrackInfo> Queue { get; private set; }
		public LavalinkNodeConnection Node { get; private set; }
		public LavalinkGuildConnection Connection { get; private set; }
		public TrackInfo NowPlaying { get; private set; }

		public IEnumerable<TrackInfo> GetQueue()
		{
			TrackInfo[] temp;

			lock (this.Queue)
				temp = this.Queue.ToArray();

			return temp;
		}
		
		public TimeSpan PlaybackPosition
		{
			get
			{
				if (!this.IsConnected)
					return TimeSpan.Zero;

				return this.Connection.CurrentState.PlaybackPosition;
			}
		}

		public MusicPlayer(DiscordGuild guild, LavalinkNodeConnection node)
		{
			this.Guild = guild;
			this.Node = node;
			this.Queue = new List<TrackInfo>();
		}

		#region Acesso a queue

		public int Count
		{
			get
			{
				var count = 0;

				lock (this.Queue)
					count = this.Queue.Count;

				return count;
			}
		}

		public TrackInfo this[int index]
		{
			get
			{
				var value = default(TrackInfo);

				lock (this.Queue)
					value = this.Queue[index];

				return value;
			}
		}
		#endregion

		public async Task InitializeAsync(DiscordChannel chn)
		{
			if (this.Connection != null)
				await this.ShutdownAsync();

			this.Connection = await this.Node.ConnectAsync(chn);
			this.Connection.PlaybackFinished += this.NotifyTrackFinished;
		}

		public async Task ShutdownAsync()
		{
			if (!this.IsConnected)
				return;

			await this.ClearQueueAsync();
			await this.Connection.DisconnectAsync();
			this.Connection.PlaybackFinished -= this.NotifyTrackFinished;
			this.Connection = default;
		}

		public Task<int> EnqueueAsync(TrackInfo track)
		{
			var count = this.Count + 1;

			lock (this.Queue)
				this.Queue.Add(track);

			return Task.FromResult(count);
		}

		public Task<TrackInfo> DequeueAsync()
		{
			lock (this.Queue)
			{
				var value = this.Queue[0];
				this.Queue.RemoveAt(0);
				return Task.FromResult(value);
			}
		}

		public Task<TrackInfo> RemoveAsync(int index)
		{
			var value = default(TrackInfo);

			lock (this.Queue)
			{
				if (index > this.Queue.Count - 1)
					index = this.Queue.Count - 1;

				if (index < 0)
					index = 0;

				value = this.Queue[index];
				this.Queue.RemoveAt(index);
			}

			return Task.FromResult(value);
		}

		public Task ShuffleAsync()
		{
			lock (this.Queue)
				this.Queue.Sort(Shuffler);

			return Task.CompletedTask;
		}

		public Task ClearQueueAsync()
		{
			lock (this.Queue)
				this.Queue.Clear();

			return Task.CompletedTask;
		}

		public async Task NextAsync()
		{
			this.NowPlaying = await this.DequeueAsync();

			if (this.NowPlaying == null)
				await this.ShutdownAsync();
			else
			{
				await this.Connection.PlayAsync(this.NowPlaying.Track);
				await this.NowPlayingObserver(this.NowPlaying);
			}
		}

		async Task NotifyTrackFinished(TrackFinishEventArgs e)
		{
			var count = this.Count; // tentando não causar um deadlock.

			if (count == 0)
				await this.ShutdownAsync();
			else
			{
				if (e.Reason.MayStartNext())
				{
					await Task.Delay(1000);
					await this.NextAsync();
				}
			}
		}

		static MusicPlayer()
		{
			Shuffler = Comparer<TrackInfo>.Create(delegate (TrackInfo track1, TrackInfo track2)
			{
				var rnd = new Random(Environment.TickCount);
				var x1 = rnd.NextDouble();
				var x2 = rnd.NextDouble();

				if (x1 > x2)
					return 1;
				else if (x1 < x2)
					return -1;
				else
					return 1;
			});
		}
	}
}
