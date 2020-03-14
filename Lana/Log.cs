using System;
using System.Collections.Generic;
using System.Linq;

namespace Lana
{
	public static class Log
	{
		static readonly object SyncLock = new object();
		static readonly IEnumerable<ConsoleColor> colors;
		static Dictionary<string, ConsoleColor> knownPrefixColors;
		static ConsoleColor lastKnownPrefixColor;

		static Log()
		{
			var temp = ((ConsoleColor[])Enum.GetValues(typeof(ConsoleColor))).ToList();
			temp.Remove(ConsoleColor.White);
			temp.Remove(ConsoleColor.Black);
			temp.Remove(ConsoleColor.Gray);
			colors = temp;
			knownPrefixColors = new Dictionary<string, ConsoleColor>();
		}

		static ConsoleColor GetColorOrCurrent(string prefix)
		{
			lock (knownPrefixColors)
			{
				if (knownPrefixColors.TryGetValue(prefix, out var color))
					return color;
				else
				{
					while (true)
					{
						var rnd = new Random(Environment.TickCount);
						var off = rnd.Next(0, colors.Count());
						color = colors.ElementAt(off);

						if (lastKnownPrefixColor != color)
							break;
					}

					lastKnownPrefixColor = color;
					knownPrefixColors[prefix] = color;
					return color;
				}
			}
		}

		static void WriteLine(string prefix, string message)
		{
			lock (SyncLock)
			{
				Console.ForegroundColor = GetColorOrCurrent(prefix);
				Console.Write($"[{prefix}] ");

				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine(message);
			}
		}

		public static void SyncConsole(Action cb)
		{
			var error = default(Exception);

			lock (SyncLock)
			{
				try { cb?.Invoke(); }
				catch (Exception ex) { error = ex; }
			}

			if (error != null)
				Error("LanaBot#Internal", $"Houve um erro na função de sincronização do console.\n{error}");
		}

		public static void Information(string prefix, string message)
			=> WriteLine(prefix, message);

		public static void Warning(string prefix, string message)
			=> WriteLine(prefix, message);

		public static void Debug(string prefix, string message)
			=> WriteLine(prefix, message);

		public static void Error(string prefix, string message)
			=> WriteLine(prefix, message);

		public static void Information<T>(string message)
			=> Information(prefix: typeof(T).Name, message);

		public static void Warning<T>(string message) =>
			Warning(prefix: typeof(T).Name, message);

		public static void Debug<T>(string message) =>
			Debug(prefix: typeof(T).Name, message);

		public static void Error<T>(string message) =>
			Error(prefix: typeof(T).Name, message);
	}
}