#pragma warning disable CS1998

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using Lana.Attributes;
using Lana.Entities.Lavalink;
using Lana.Entities.Music;
using Lana.Services;

namespace Lana.Modules
{
	[RequireGuild, ModuleLifespan(ModuleLifespan.Transient)]
	public class MusicModule : BaseCommandModule
	{
		[DontInject]
		protected MusicService Service { get; set; }

		[DontInject]
		protected MusicPlayer Player { get; set; }

		public MusicModule(MusicService service)
		{
			this.Service = service;
		}

		async Task NowPlaying(TrackInfo ctx)
		{
			var track = ctx.Track;
			await ctx.Channel.SendMessageAsync($":notes: Tocando agora **{Formatter.Sanitize(track.Title)}** " +
				$"(`{track.Length.Format()}`)");
		}

		public override async Task BeforeExecutionAsync(CommandContext ctx)
		{
			var mbr = ctx.Member.VoiceState?.Channel;
			var vst = ctx.Guild.CurrentMember.VoiceState?.Channel;

			if (this.Player == null)
				this.Player = await this.Service.GetOrCreateAsync(ctx.Guild);

			if (this.Player != null)
				this.Player.NowPlayingObserver = this.NowPlaying;

			if (!this.Player.IsConnected)
			{
				if (mbr == null)
					return;

				if (vst == null)
					await this.Player.InitializeAsync(mbr);
			}

			if (this.Player != null && this.Player.Connection == null)
				await this.Player.InitializeAsync(mbr);
		}

		public override Task AfterExecutionAsync(CommandContext ctx)
		{
			this.Player = null;
			this.Service = null;
			return base.AfterExecutionAsync(ctx);
		}

		[Command, RequireVoiceChannel, RequireSameVoiceChannel, RequireRoles(RoleCheckMode.Any, "Administrador")]
		public async Task Clear(CommandContext ctx)
		{
			await this.Player.ClearQueueAsync();
			await ctx.RespondAsync(":white_check_mark: Fila limpa. A música atual irá continuar a tocar.");
		}

		[Command, RequireVoiceChannel, RequireSameVoiceChannel, Priority(0)]
		public async Task Play(CommandContext ctx, [RemainingText] string search)
		{
			var waiting = await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
				.WithDescription($"{Emotes.LoadingYellow} Pesquisando músicas..."));

			var lavalinkResult = await this.Service.GetTracksAsync(search);

			if (lavalinkResult.Tracks?.Count() == 0)
			{
				await waiting.DeleteAsync().Safe();
				await ctx.RespondAsync($"{ctx.User.Mention} :x: Nenhum resultado encontrado para pesquisa!");
				return;
			}

			var selector = new TrackSelector(ctx, lavalinkResult.Tracks, search);
			await waiting.DeleteAsync().Safe();
			var selectorResult = await selector.SelectAsync();

			if (selectorResult.Status == TrackSelectorStatus.Cancelled)
				return;
			else if (selectorResult.Status == TrackSelectorStatus.TimedOut)
			{
				await ctx.RespondAsync($"{ctx.User.Mention} :x: Tempo limite esgotado!");
				return;
			}

			var result = selectorResult.Result;
			var index = await Player.EnqueueAsync(result);
			await ctx.RespondAsync($":headphones: A música **{Formatter.Sanitize(result.Track.Title)}** ({Formatter.Sanitize(result.Track.Length.Format())}) pedida por {ctx.User.Mention} foi adicionada à fila! `[#{index}]`");

			if (this.Player.NowPlaying == null)
				await this.Player.NextAsync();
		}

		[Command, RequireVoiceChannel, RequireSameVoiceChannel, Priority(1)]
		public async Task Play(CommandContext ctx, Uri url)
		{
			var waiting = await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
				.WithDescription($"{Emotes.LoadingYellow} Obtendo músicas..."));

			var lavalinkResult = await this.Service.GetTracksAsync(url);
			await waiting.DeleteAsync().Safe();

			if (lavalinkResult.Tracks?.Count() == 0)
			{
				await ctx.RespondAsync($"{ctx.User.Mention} :x: Url fornecida é inválida ou o link está quebrado!");
				return;
			}

			if (lavalinkResult.LoadResultType == LavalinkLoadResultType.TrackLoaded)
			{
				var track = lavalinkResult.Tracks.First();
				var index = await this.Player.EnqueueAsync(new TrackInfo(ctx.Channel, ctx.User, track));
				await ctx.RespondAsync($":headphones: A música **{Formatter.Sanitize(track.Title)}** ({Formatter.Sanitize(track.Length.Format())}) pedida por {ctx.User.Mention} foi adicionada à fila! `[#{index}]`");
			}
			else if (lavalinkResult.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
			{
				var tasks = new List<Task>();
				var count = 0;

				foreach (var it in lavalinkResult.Tracks)
				{
					count++;
					tasks.Add(this.Player.EnqueueAsync(new TrackInfo(ctx.Channel, ctx.User, it)));
				}

				// TODO pagination pra ver as musicas
				await Task.WhenAll(tasks);
				await ctx.RespondAsync($":headphones: Foram adicionada(s) {count:#,#} músicas na fila.");
			}
			else
			{
				await ctx.RespondAsync($"{ctx.User.Mention} :x: Url fornecida é inválida ou o link está quebrado!");
				return;
			}

			if (this.Player.NowPlaying == null)
				await this.Player.NextAsync();
		}

		[Command, Aliases("np")]
		public async Task NowPlaying(CommandContext ctx)
		{
			if (this.Player.NowPlaying == null)
				await ctx.RespondAsync(":x: Nenhuma música está sendo tocada neste momento!");
			else
			{
				var nowplaying = this.Player.NowPlaying;
				var imageURL = $"https://img.youtube.com/vi/{nowplaying.Track.Uri.ToString().Replace("https://www.youtube.com/watch?v=", "")}/maxresdefault.jpg";

				await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
					.WithTitle(":headphones: Tocando agora")
					.WithDescription(Formatter.Bold(Formatter.Sanitize(nowplaying.Track.Title)) + " por " + Formatter.Sanitize(nowplaying.Track.Author))
					.AddField("Posição e Duração", this.Player.PlaybackPosition.ToString("mm\\:ss") + " — " + nowplaying.Track.Length.ToString("mm\\:ss"), true)
					.AddField("Pedido por", nowplaying.User.Mention, true)
					.AddField("Canal", nowplaying.Channel.Mention, true)
					.WithColor(DiscordColor.Blurple)
					.WithThumbnailUrl(imageURL));
			}
		}
		[Command, Aliases("q", "fila")]
		public async Task Queue(CommandContext ctx)
		{
			if (this.Player.NowPlaying == null)
			{
				await ctx.RespondAsync(":x: Nenhuma música está sendo tocada neste momento!");
				return;
			}

			var offset = 0;
			var description = string.Empty;
			var interactivity = ctx.Client.GetInteractivity();
			var pages = new List<Page>();

			foreach (var track in this.Player.GetQueue())
			{
				if (description.Length > 1500)
				{
					pages.Add(new Page
					{
						Embed = new DiscordEmbedBuilder()
							.WithColor(DiscordColor.Blurple)
							.WithAuthor("Fila de Músicas", iconUrl: ctx.Client.CurrentUser.AvatarUrl)
							.WithDescription(description)
					});

					description = string.Empty;
				}

				string prefix;

				if (this.Player.NowPlaying == track)
					prefix = Emotes.MsPlay;
				else
					prefix = $"**`[#{offset + 1}:00]`**";

				description += $"{prefix} {Formatter.Bold(Formatter.Sanitize(track.Track.Title.StrTruncate(34)))}" +
					$" pedido por {track.User.Mention} (`{track.Track.Length.Format()}`)\n";

				offset++;
			}

			if (pages.Count > 1)
				await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
			else
				await ctx.RespondAsync(embed: pages[0].Embed);
		}
	}
}
