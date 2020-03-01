using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;

public static class DSharpPlusExtensions
{
	public static bool TryGetFailedCheck<TFailedCheck>(this ChecksFailedException exception, out TFailedCheck obj)
			where TFailedCheck : CheckBaseAttribute
	{
		obj = exception.FailedChecks.FirstOrDefault(x => x is TFailedCheck) as TFailedCheck;
		return obj != null;
	}

	public static bool HasFailedCheck<TFailedCheck>(this ChecksFailedException exception)
			where TFailedCheck : CheckBaseAttribute
	{
		return TryGetFailedCheck<TFailedCheck>(exception, obj: out var _);
	}

	public static DiscordEmoji LookupEmoji(string name)
	{
		var objRef = typeof(DiscordEmoji).GetProperty("UnicodeEmojis", BindingFlags.NonPublic | BindingFlags.Static)
			.GetValue(null);

		var dictionary = ((IReadOnlyDictionary<string, string>)objRef);

		if (!dictionary.TryGetValue(name, out var entity))
			return default;

		return DiscordEmoji.FromUnicode(entity);
	}

	public static string Format(this DiscordUser user)
		=> $"{user.Username}#{user.Discriminator}";

	public static string SanetizeEx(this string text)
	{
		text = text.Replace("[", string.Empty);
		text = text.Replace("]", string.Empty);
		text = Formatter.Sanitize(text);
		return text;
	}
}
