using System;
using DSharpPlus;
using DSharpPlus.Entities;

public static class Emotes
{
	public static readonly Emote LoadingYellow = ("<a:SmallLoadYellow:594249301295890463>", 594249301295890463);
	public static readonly Emote MsPlay = ("<:MsPlay:683459574417850380>", 683459574417850380);
	public static readonly Emote Tab = ("<:tab:677871914886234143>", 677871914886234143);
}


public class Emote : IEquatable<Emote>
{
	public Emote(string raw, ulong id)
	{
		this.Name = raw;
		this.Id = id;
	}

	public ulong Id { get; }
	public string Name { get; }

	public static implicit operator string(Emote e)
		=> e.Name;

	public static implicit operator ulong(Emote e)
		=> e.Id;

	public override int GetHashCode()
	{
		return this.Id.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		return Equals(obj as Emote);
	}

	public bool Equals(Emote other)
	{
		if (other is null)
			return false;

		if (ReferenceEquals(other, this))
			return true;

		return this.Id == other.Id;
	}

	public override string ToString()
	{
		return this.Name;
	}

	public DiscordEmoji ToGuildEmote(DiscordClient client)
	{
		return DiscordEmoji.FromGuildEmote(client, this.Id);
	}

	public DiscordEmoji ToDiscordEmoji(DiscordClient client)
	{
		return DiscordEmoji.FromName(client, this.Name);
	}

	public static implicit operator Emote(ValueTuple<string, ulong> vt)
		=> new Emote(vt.Item1, vt.Item2);


	public static bool operator ==(Emote e1, Emote e2)
		=> Equals(e1, e2);

	public static bool operator !=(Emote e1, Emote e2)
		=> !(e1 == e2);
}