using System;

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
}
