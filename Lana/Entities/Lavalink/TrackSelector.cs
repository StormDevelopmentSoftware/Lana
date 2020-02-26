using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;

namespace Lana.Entities.Lavalink
{
	public class TrackSelector
	{
		static readonly IReadOnlyDictionary<int, DiscordEmoji> NumberMapping;
		static readonly IReadOnlyDictionary<DiscordEmoji, int> NumberMappingReversed;

		static TrackSelector()
		{
			NumberMapping = new Dictionary<int, DiscordEmoji>
			{
				[1] = DSharpPlusExtensions.LookupEmoji(":one:"),
				[2] = DSharpPlusExtensions.LookupEmoji(":two:"),
				[3] = DSharpPlusExtensions.LookupEmoji(":three:"),
				[4] = DSharpPlusExtensions.LookupEmoji(":four:"),
				[5] = DSharpPlusExtensions.LookupEmoji(":five:"),
				[6] = DSharpPlusExtensions.LookupEmoji(":six:"),
				[7] = DSharpPlusExtensions.LookupEmoji(":seven:"),
				[8] = DSharpPlusExtensions.LookupEmoji(":eight:"),
				[9] = DSharpPlusExtensions.LookupEmoji(":nine:")
			};

			NumberMappingReversed = NumberMapping
				.ToDictionary(x => x.Value, x => x.Key);
		}

		private CommandContext context;
		private InteractivityExtension interactivity;
		private IEnumerable<LavalinkTrack> tracks;
		private string search;
		private int count;

		public TrackSelector(CommandContext ctx, IEnumerable<LavalinkTrack> tracks, string search)
		{
			this.context = ctx;
			this.interactivity = this.context.Client.GetInteractivity();
			this.count = Math.Min(tracks.Count(), 5);
			this.search = search;
			this.tracks = tracks.Take(this.count);
		}

		public async Task<TrackSelectorResult> SelectAsync()
		{
			var result = new TrackSelectorResult { Status = TrackSelectorStatus.TimedOut };
			{
				if (this.count == 1)
				{
					result.CurrentTrack = new TrackInfo(this.context.Channel, this.context.User, this.tracks.First());
					result.Status = TrackSelectorStatus.Success;
					return result;
				}

				string description = string.Empty;

				for (var i = 0; i < this.count; i++)
				{
					var track = this.tracks.ElementAt(i);
					var name = track.Title;

					if (name.Length > 64)
						name = name.Substring(0, 64) + "...";

					description += string.Concat(NumberMapping[i + 1], " ",
						Formatter.MaskedUrl(Formatter.Sanitize(name), track.Uri, track.Author), "\n");
				}

				var builder = new DiscordEmbedBuilder()
					.WithTitle($":mag: Resultados da pesquisa para __**{Formatter.Sanitize(this.search)}**__")
					.WithDescription(description)
					.WithFooter($"Solicitado por {this.context.User.Format()}", this.context.User.AvatarUrl)
					.WithTimestamp(DateTime.Now)
					.WithColor(DiscordColor.Blurple);

				var msg = await this.context.RespondAsync(embed: builder);

				for (var i = 0; i < this.count; i++)
					await msg.CreateReactionAsync(NumberMapping[i + 1]);

				var X = DiscordEmoji.FromUnicode("❌");
				await msg.CreateReactionAsync(X);

				var response = await this.interactivity.WaitForReactionAsync(x =>
					x.Message == msg && x.User == this.context.User
						&& (NumberMappingReversed.ContainsKey(x.Emoji) || x.Emoji == X));

				try { await msg.DeleteAsync(); }
				catch { }

				if (response.TimedOut)
					return result;
				else
				{
					if (response.Result.Emoji == X)
					{
						result.Status = TrackSelectorStatus.Cancelled;
						return result;
					}
					var option = NumberMappingReversed[response.Result.Emoji];
					var track = this.tracks.ElementAt(option - 1);
					result.Status = TrackSelectorStatus.Success;
					result.CurrentTrack = new TrackInfo(this.context.Channel, this.context.User, track);
					return result;
				}
			}
		}
	}
}
