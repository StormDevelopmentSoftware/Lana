using System;
using System.Threading.Tasks;

public static class DotNetExtensions
{
	public static string Format(this TimeSpan ts)
	{
		var result = string.Empty;

		if (ts.TotalSeconds >= 1)
		{
			if (ts.TotalHours >= 1)
				result += $"{ts.Hours}h ";

			if (ts.TotalMinutes >= 1)
				result += $"{ts.Minutes}m ";

			if (ts.TotalSeconds >= 1)
				result += $"{ts.Seconds}s ";
		}
		else
			result += $"{ts.Milliseconds}ms";

		return result.TrimEnd();
	}

	public static async Task Safe(this Task t)
	{
		try { await t; }
		catch { }
	}

	public static async Task<TResult> Safe<TResult>(this Task<TResult> t)
	{
		var def = default(TResult);

		try { def = await t; }
		catch { }

		return def;
	}

	public static string StrTruncate(this string text, int maxLimit = 12)
	{
		if (text.Length < maxLimit)
			return text;

		return $"{text.Substring(0, text.Length - 3)}...";
	}
}
