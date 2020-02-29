using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Lana.Attributes;

namespace Lana.Modules
{
	public class AdministrationModule : BaseCommandModule
	{
		[RequireBotDeveloper]
		[RequireRoles(RoleCheckMode.Any, "Administrador")]
		[Command("bulkdelete")]
		public async Task BulkDeleteAsync(CommandContext ctx, [Description("Quantidade")] int? amount = null)
		{
			if (amount == null)
			{
				await ctx.RespondAsync(":x: É necessária uma quantiadade de mensagens para deletar.");
				return;
			}

			var messages = await ctx.Channel.GetMessagesAsync(amount.Value);
			await ctx.Channel.DeleteMessagesAsync(messages, "Lana bulk delete");

			var success = await ctx.RespondAsync(":white_check_mark: As mensagens foram deletadas com sucesso.\n*Esta mensagem irá se auto-destruir em cinco segundos.*");
			await Task.Delay(5000);
			await success.DeleteAsync();
		}

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
