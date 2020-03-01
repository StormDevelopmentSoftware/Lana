using System;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;

namespace Lana.Entities.Lavalink
{
	public class TrackInfo : IEquatable<TrackInfo>
	{
		public TrackInfo(DiscordChannel chn, DiscordUser usr, LavalinkTrack track)
		{
			this.Channel = chn;
			this.User = usr;
			this.Track = track;
		}

		public DiscordChannel Channel { get; private set; }
		public DiscordUser User { get; private set; }
		public LavalinkTrack Track { get; private set; }

		public override int GetHashCode()
		{
			return this.Track.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			return this.Equals(obj as TrackInfo);
		}

		public bool Equals(TrackInfo other)
		{
			if (other is null)
				return false;

			if (ReferenceEquals(other, this))
				return true;

			return this.Channel == other.Channel
				&& this.User == other.User
				&& this.Track == other.Track;
		}

		public static bool operator ==(TrackInfo t1, TrackInfo t2)
			=> Equals(t1, t2);

		public static bool operator !=(TrackInfo t1, TrackInfo t2)
			=> !(t1 == t2);
	}
}
