using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Lana.Attributes;

namespace Lana
{
	public class LanaEvents
	{
		private LanaBot bot;
		private DiscordClient discord;

		public LanaEvents(LanaBot bot)
		{
			this.bot = bot;
			this.discord = this.bot.Discord;
		}

		private const ulong LogGuildId = 681496938092429357;
		private const ulong LogChannelId = 681940088045043717;
		private DiscordChannel logChannel;

		public Task InitializeAsync()
		{
			this.discord.GuildDownloadCompleted += this.NotifyGuildDownloadCompleted;
			this.discord.MessageDeleted += this.NotifyMessageDeleted;
			this.discord.MessageUpdated += this.NotifyMessageUpdated;
			this.bot.CommandsNext.CommandErrored += this.NotifyCommandFailed;
			return Task.CompletedTask;
		}

		protected Task NotifyGuildDownloadCompleted(GuildDownloadCompletedEventArgs e)
		{
			var guild = e.Guilds.Select(x => x.Value)
				.Where(x => x.Id == LogGuildId)
				.FirstOrDefault();

			if (guild == null)
			{
				Console.ForegroundColor = ConsoleColor.Magenta;
				Console.Write("[LANA]");
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(" Não foi possível determinar a guilda de moderação.\n");
				Console.ResetColor();
				return Task.CompletedTask;
			}

			this.logChannel = guild.GetChannel(LogChannelId);

			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.Write("[LANA]");
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine($" Guild de moderação reconhecida: {guild.Name}: #{this.logChannel.Name}");
			Console.ResetColor();

			return Task.CompletedTask;
		}

		protected Task NotifyMessageUpdated(MessageUpdateEventArgs e)
		{
			if (e.Author.IsBot)
				return Task.CompletedTask;

			if (this.logChannel == null)
				return Task.CompletedTask;

			_ = Task.Run(() => this.logChannel.SendMessageAsync(embed: new DiscordEmbedBuilder()
				.WithTitle("Mensagem editada")
				.WithColor(DiscordColor.Yellow)
				.WithDescription($"{e.Author.Username}#{e.Author.Discriminator} (ID {e.Author.Id}) mudou a sua mensagem no canal {e.Channel.Mention} de: {Formatter.BlockCode(Formatter.Sanitize(e.MessageBefore.Content))} para: {Formatter.BlockCode(Formatter.Sanitize(e.Message.Content))}")
				.WithTimestamp(DateTime.Now)
				.WithFooter($"{e.Author.Username}#{e.Author.Discriminator}", e.Author.AvatarUrl)));

			return Task.CompletedTask;
		}

		protected Task NotifyMessageDeleted(MessageDeleteEventArgs e)
		{
			if (e.Message.Author?.IsBot == true)
				return Task.CompletedTask;

			if (this.logChannel == null)
				return Task.CompletedTask;

			_ = Task.Run(() => this.logChannel.SendMessageAsync(embed: new DiscordEmbedBuilder()
				.WithTitle("Mensagem deletada")
				.WithColor(DiscordColor.Red)
				.WithDescription($"A mensagem de {e.Message.Author.Username}#{e.Message.Author.Discriminator} (ID {e.Message.Author.Id}) em {e.Channel.Mention} foi apagada: {Formatter.BlockCode(Formatter.Sanitize(e.Message.Content))}")
				.WithTimestamp(DateTime.Now)
				.WithFooter($"{e.Message.Author.Username}#{e.Message.Author.Discriminator}", e.Message.Author.AvatarUrl)));

			return Task.CompletedTask;
		}

		protected async Task NotifyCommandFailed(CommandErrorEventArgs e)
		{
			var ctx = e.Context;
			var ex = e.Exception;

			while (ex is AggregateException)
				ex = ex.InnerException;

			if (ex is ChecksFailedException checks)
			{
				if (checks.HasFailedCheck<RequireVoiceChannelAttribute>())
				{
					await ctx.RespondAsync($"{ctx.User.Mention} :x: Você precisa estar conectado em um canal de voz.");
					return;
				}
				else if (checks.HasFailedCheck<RequireSameVoiceChannelAttribute>())
				{
					await ctx.RespondAsync($"{ctx.User.Mention} :x: Você precisa estar conectado no mesmo canal de voz que eu.");
					return;
				}
				else if (checks.HasFailedCheck<RequireBotDeveloperAttribute>())
				{
					await ctx.RespondAsync($"{ctx.User.Mention} :x: Você não é desenvolvedor do bot.");
					return;
				}
			}
			else
			{
				if (ex is CommandNotFoundException) return;
				else if (ex.Message.Contains("Could not find a suitable overload for the command.")) return;

				Console.ForegroundColor = ConsoleColor.DarkBlue;
				Console.Write("[COMMANDS] ".PadRight(12));

				Console.ForegroundColor = ConsoleColor.Blue;
				Console.Write("Execução do comando falhou!\n");

				var props = new Dictionary<string, string>
				{
					["Comando"] = ctx.Command.QualifiedName,
					["Usuário"] = $"{ctx.User.Username}#{ctx.User.Discriminator}",
					["Canal"] = $"#{ctx.Channel.Name}",
					["Guild"] = ctx.Guild.Name,
					["Descrição"] = ex.Message,
					["Classe"] = ex.GetType().FullName,
					["StackTrace"] = ex.StackTrace + "\n"
				};

				foreach (var (key, value) in props)
				{
					Console.ForegroundColor = ConsoleColor.Cyan;
					Console.Write(key.PadRight(12));

					Console.ForegroundColor = ConsoleColor.Gray;
					Console.Write(":");

					Console.ForegroundColor = ConsoleColor.DarkYellow;
					Console.Write(" " + value);

					Console.ForegroundColor = ConsoleColor.White;
					Console.Write("\n");
				}

				Console.Write("\n");

				await ctx.RespondAsync($"[`{ex.GetType().Name}`] {ctx.User.Mention} :x: Um erro ocorreu durante a execução do comando.");
			}
		}
	}
}
