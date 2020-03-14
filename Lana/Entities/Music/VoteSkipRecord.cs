using System;
using System.Diagnostics;
using DSharpPlus.Entities;

namespace Lana.Entities.Music
{
	[DebuggerDisplay("{ToString(),nq}")]
	public class VoteSkipRecord : IEquatable<VoteSkipRecord>
	{
		public VoteSkipRecord(DiscordUser user, bool state)
		{
			this.User = user;
			this.State = state;
		}

		public DiscordUser User { get; }
		public bool State { get; }

		public override bool Equals(object obj)
		{
			return Equals(obj as VoteSkipRecord);
		}

		public override int GetHashCode()
		{
			return this.User.GetHashCode();
		}

		public override string ToString()
		{
			return $"VoteSkipRecord: User: {this.User.Username}#{this.User.Discriminator}; State: {this.State}";
		}

		public bool Equals(VoteSkipRecord other)
		{
			if (other is null)
				return false;

			if (ReferenceEquals(other, this))
				return true;

			// deve comparar se o voto é o mesmo usuário idependente se for a favor ou contra,
			// portanto somente o usuário é comaprado.
			return this.User == other.User;
		}
	}
}
