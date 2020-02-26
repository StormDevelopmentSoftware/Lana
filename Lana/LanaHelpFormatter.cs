using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace Lana
{
	public class LanaHelpFormatter : BaseHelpFormatter
	{
		public DiscordEmbedBuilder EmbedBuilder { get; }
		private Command command { get; set; }
		private CommandContext context { get; set; }

		public LanaHelpFormatter(CommandContext ctx) : base(ctx)
		{
			context = ctx;
			this.EmbedBuilder = new DiscordEmbedBuilder()
				.WithFooter($"LanaBot • versão 1.0")
				.WithColor(DiscordColor.Blurple);
		}

		public override CommandHelpMessage Build()
		{
			if (this.command == null)
			{
				this.EmbedBuilder.WithAuthor("Ajuda", iconUrl: context.Client.CurrentUser.AvatarUrl).WithDescription("Use `" + context.Prefix + "help [command]` para mais informações sobre um comando.");
			}
			else this.EmbedBuilder.WithAuthor("Ajuda - Comando " + command.Name, iconUrl: context.Client.CurrentUser.AvatarUrl);
			return new CommandHelpMessage(embed: EmbedBuilder.Build());
		}

		public override BaseHelpFormatter WithCommand(Command command)
		{
			this.command = command;
			this.EmbedBuilder.WithDescription($"{Formatter.InlineCode(command.Name)}: {command.Description ?? "Sem descrição disponível."}");

			if (command is CommandGroup cg && cg.IsExecutableWithoutSubcommands)
			{
				this.EmbedBuilder.WithDescription($"{this.EmbedBuilder.Description}\nEste grupo pode ser executado.");
			}

			if (command.Aliases?.Any() == true)
			{
				this.EmbedBuilder.AddField("Aliases", string.Join(", ", command.Aliases.Select(Formatter.InlineCode)));
			}

			if (command.Overloads?.Any() == true)
			{
				var sb = new StringBuilder();

				foreach (var ovl in command.Overloads.OrderByDescending(x => x.Priority))
				{
					sb.Append('`').Append(command.QualifiedName);

					foreach (var arg in ovl.Arguments)
						sb.Append(arg.IsOptional || arg.IsCatchAll ? " [" : " <").Append(arg.Name).Append(arg.IsCatchAll ? "..." : "").Append(arg.IsOptional || arg.IsCatchAll ? ']' : '>');

					sb.Append("`\n");

					foreach (var arg in ovl.Arguments)
						sb.Append('`').Append(arg.Name).Append(" (").Append(this.CommandsNext.GetUserFriendlyTypeName(arg.Type)).Append(")`: ").Append(arg.Description ?? "Sem descrição disponível.").Append('\n');

					sb.Append('\n');
				}
				this.EmbedBuilder.AddField("Argumentos", sb.ToString(), false);
			}
			return this;

		}

		public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
		{
			this.EmbedBuilder.AddField(this.command != null ? "Subcomandos" : "Comandos", string.Join(", ", subcommands.Select(x => Formatter.InlineCode(x.Name))), false);

			return this;
		}
	}
}
