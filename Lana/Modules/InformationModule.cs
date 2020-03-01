using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace Lana.Modules
{
	public class InformationModule : BaseCommandModule
	{
		[Command("ping")]
		public async Task PingAsync(CommandContext ctx)
		{
			var watch = Stopwatch.StartNew();
			await ctx.TriggerTypingAsync();
			watch.Stop();

			await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
				.WithAuthor("Ping", iconUrl: ctx.Client.CurrentUser.AvatarUrl)
				.AddField(":zap: Rest", $"{watch.ElapsedMilliseconds}ms", true)
				.AddField(":satellite_orbital: Gateway", $"{ctx.Client.Ping}ms", true)
				.WithColor(DiscordColor.Blurple)
				.WithTimestamp(DateTime.Now));
		}
	}
}
