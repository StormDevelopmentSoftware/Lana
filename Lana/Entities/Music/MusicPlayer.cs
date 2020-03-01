using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Lana.Entities.Lavalink;

namespace Lana.Entities.Music
{
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

			this.NowPlaying = default;
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
