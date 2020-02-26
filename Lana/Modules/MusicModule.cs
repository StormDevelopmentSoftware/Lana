﻿#pragma warning disable CS1998

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Net;
using Lana.Attributes;
using Lana.Entities.Lavalink;

namespace Lana.Modules
{
	[RequireGuild]
	public class MusicModule : BaseCommandModule
	{
		private LavalinkNodeConnection node;
		private LavalinkGuildConnection connection;
		private ConcurrentQueue<TrackInfo> tracks;
		private TrackInfo currentTrack;
		private LavalinkConfiguration config;
		private LanaBot bot;

		public MusicModule(LanaBot bot)
		{
			this.bot = bot;
			this.tracks = new ConcurrentQueue<TrackInfo>();
			this.currentTrack = default;
			this.bot.Lavalink.NodeDisconnected += this.NotifyNodeDisconnected;
			this.UpdateConfiguration();
		}

		public void UpdateConfiguration()
			=> this.config = new LavalinkConfiguration(this.bot.Configuration.Lavalink.Build());

		[Command, Aliases("np")]
		public async Task NowPlaying(CommandContext ctx)
		{
			if (currentTrack != null)
			{
				var imageURL = $"https://img.youtube.com/vi/{currentTrack.Track.Uri.ToString().Replace("https://www.youtube.com/watch?v=", "")}/maxresdefault.jpg";
				await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
					.WithTitle(":headphones: Tocando agora")
					.AddField("Título e autor", Formatter.Bold(Formatter.Sanitize(currentTrack.Track.Title)) + " por " + Formatter.Sanitize(currentTrack.Track.Author), true)
					.AddField("Posição e Duração", currentTrack.Track.Position.ToString("mm\\:ss") + " — " + currentTrack.Track.Length.ToString("mm\\:ss"), false)
					.AddField("Pedido por", currentTrack.User.Mention, true)
					.AddField("Canal", currentTrack.Channel.Mention, true)
					.WithColor(DiscordColor.Blurple)
					.WithThumbnailUrl(imageURL));

			}
			else await ctx.RespondAsync(":x: Nenhuma música está sendo tocada neste momento!");
		}
[Command, Aliases("q", "fila")]
        public async Task Queue(CommandContext ctx)
        {
            if (currentTrack == null)
            {
                await ctx.RespondAsync(":x: Nenhuma música está sendo tocada neste momento!");
                return;
            }

            var interactivity = ctx.Client.GetInteractivity();
            
            var sb = new StringBuilder();

            sb.Append($"[**`▶️ AGORA`**] {Formatter.Bold(Formatter.Sanitize(currentTrack.Track.Title))} pedido por {currentTrack.User.Mention} (`{currentTrack.Track.Length.Format()}`)\n");

            int index = 1;
            foreach (var track in tracks)
            {
                sb.Append($"[**`#{index}`**] {Formatter.Bold(Formatter.Sanitize(track.Track.Title))} pedido por {track.User.Mention} (`{track.Track.Length.Format()}`)\n");
                index++;
            }
            var embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Blurple)
                .WithAuthor("Fila de Músicas", iconUrl: ctx.Client.CurrentUser.AvatarUrl);

            var pages = interactivity.GeneratePagesInEmbed(sb.ToString(), SplitType.Line, embed);
            await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pages);
        }
        
		[Command, RequireVoiceChannel, RequireSameVoiceChannel, Priority(0)]
		public async Task Play(CommandContext ctx, [RemainingText] string search)
		{
			var lpWait = await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
				.WithAuthor("Obtendo pesquisa de músicas...", iconUrl: "https://i.imgur.com/HACGw6c.gif"));

			await ctx.TriggerTypingAsync();

			var response = await this.node.Rest.GetTracksAsync(search, LavalinkSearchType.Youtube);

			if (response.Tracks?.Count() == 0)
			{
				await lpWait.DeleteAsync().Safe();
				await ctx.RespondAsync($"{ctx.User.Mention} :x: Nenhum resultado encontrado para pesquisa!");
				return;
			}

			var selector = new TrackSelector(ctx, response.Tracks, search);
			var result = await selector.SelectAsync();
			await lpWait.DeleteAsync().Safe();

			if (result.Status == TrackSelectorStatus.Cancelled)
			{
				return;
			}
			else if (result.Status == TrackSelectorStatus.TimedOut)
			{
				await ctx.RespondAsync($"{ctx.User.Mention} :x: Tempo limite esgotado!");
				return;
			}

			var pos = this.tracks.Count + 1;
			var selectedTrack = result.CurrentTrack.Track;
			this.tracks.Enqueue(result.CurrentTrack);

			await ctx.RespondAsync($":headphones: A música **{Formatter.Sanitize(selectedTrack.Title)}** ({Formatter.Sanitize(selectedTrack.Length.Format())}) pedida por {ctx.User.Mention} foi adicionada à fila! `[#{pos}]`");

			if (this.currentTrack == null)
				await this.NotifyNextTrackAsync();

			//if (op == 0)
			//{
			//    await ctx.RespondAsync(ctx.User.Mention, embed: new DiscordEmbedBuilder()
			//        .WithColor(DiscordColor.Blurple)
			//        .WithDescription($":notes: Tocando agora **{Formatter.Sanitize(selectedTrack.Title)}** ({Formatter.Sanitize(selectedTrack.Length.Format())})"));
			//}
			//else if (op == 1)
			//{
			//    await ctx.RespondAsync(ctx.User.Mention, embed: new DiscordEmbedBuilder()
			//        .WithColor(DiscordColor.Blurple)
			//        .WithDescription($":headphones: A música **{Formatter.Sanitize(selectedTrack.Title)}** ({Formatter.Sanitize(selectedTrack.Length.Format())}) foi adicionada à fila! `[#{pos}]`"));
			//}
			//else
			//{
			//    await ctx.RespondAsync(ctx.User.Mention, embed: new DiscordEmbedBuilder()
			//        .WithColor(DiscordColor.Red)
			//        .WithDescription($"{ctx.User.Mention} :x: Lavalink não disponível!"));
			//}
		}

		[Command, RequireVoiceChannel, RequireSameVoiceChannel, Priority(1)]
		public async Task Play(CommandContext ctx, Uri url)
		{
			var lpWait = await ctx.RespondAsync(embed: new DiscordEmbedBuilder()
				.WithAuthor("Obtendo pesquisa de músicas...", iconUrl: "https://i.imgur.com/HACGw6c.gif"));

			await ctx.TriggerTypingAsync();

			var response = await this.node.Rest.GetTracksAsync(url);

			if(response.Tracks?.Count() == 0)
			{
				await lpWait.DeleteAsync().Safe();
				await ctx.RespondAsync(ctx.User.Mention, embed: new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Red)
					.WithDescription($"[`{response.LoadResultType}`] URL fornecida é inválida!"));

				return;
			}

			await lpWait.DeleteAsync().Safe();

			if(response.LoadResultType == LavalinkLoadResultType.TrackLoaded)
			{
				var selectedTrack = response.Tracks.First();
				var pos = this.tracks.Count;
				this.tracks.Enqueue(new TrackInfo(ctx.Channel, ctx.User, selectedTrack));
				await ctx.RespondAsync($":headphones: A música **{Formatter.Sanitize(selectedTrack.Title)}** ({Formatter.Sanitize(selectedTrack.Length.Format())}) pedida por {ctx.User.Mention} foi adicionada à fila! `[#{pos}]`");
			}
			else if(response.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
			{
				var count = 0;

				foreach(var selectedTrack in response.Tracks)
				{
					count++;
					this.tracks.Enqueue(new TrackInfo(ctx.Channel, ctx.User, selectedTrack));
				}

				await ctx.RespondAsync($":headphones: Foram adicionada(s) {count:#,#} músicas na fila."); // TODO pagination pra ver as musicas
			}
			else
			{
				await ctx.RespondAsync(ctx.User.Mention, embed: new DiscordEmbedBuilder()
					.WithColor(DiscordColor.Red)
					.WithDescription($"[`{response.LoadResultType}`] URL fornecida é inválida!"));

				return;
			}

			if (this.currentTrack == null)
				await this.NotifyNextTrackAsync();
		}

		protected async Task NotifyTrackFinished(TrackFinishEventArgs e)
		{
			await Task.Delay(1000);
			await this.NotifyNextTrackAsync();
		}

		protected async Task NotifyNextTrackAsync()
		{
			if (this.tracks.TryDequeue(out this.currentTrack))
			{
				await this.connection.PlayAsync(this.currentTrack.Track);

				try
				{
					var selectedTrack = this.currentTrack.Track;

					await this.currentTrack.Channel.SendMessageAsync($":notes: Tocando agora **{Formatter.Sanitize(selectedTrack.Title)}** ({Formatter.Sanitize(selectedTrack.Length.Format())})");
					await Task.Delay(250);

				}
				catch { }
			}
			else
			{
				await this.connection.DisconnectAsync();
			}
		}

		//protected async Task<int> PlayTrackAsync(TrackInfo track)
		//{
		//    if (this.connection == null)
		//        return -1;

		//    if (this.currentTrack != null)
		//    {
		//        this.tracks.Enqueue(track);
		//        return 1;
		//    }
		//    else
		//    {
		//        this.currentTrack = track;
		//        await this.NotifyNextTrackAsync();
		//        return 0;
		//    }
		//}

		protected Task NotifyNodeDisconnected(NodeDisconnectedEventArgs e)
		{
			if (e.LavalinkNode == this.node)
				this.connection = default;

			return Task.CompletedTask;
		}

		public override async Task BeforeExecutionAsync(CommandContext ctx)
		{
			this.UpdateConfiguration();

			var lavalink = ctx.Client.GetLavalink();

			if (this.node == null || !this.node.IsConnected)
				this.node = lavalink.GetNodeConnection(this.bot.Configuration.Lavalink.RestEndpoint);

			if (this.node == null || !this.node.IsConnected)
				this.node = await lavalink.ConnectAsync(this.config);

			if (this.node == null || !this.node.IsConnected)
			{
				var objRef = lavalink.GetType().GetProperty("ConnectedNodes",
					BindingFlags.NonPublic | BindingFlags.Instance).GetValue(lavalink);

				((ConcurrentDictionary<ConnectionEndpoint, LavalinkNodeConnection>)objRef)
					.TryRemove(this.bot.Configuration.Lavalink.RestEndpoint, out var _);

				this.node = await lavalink.ConnectAsync(this.config);
			}

			var botVoiceChannel = ctx.Guild.CurrentMember.VoiceState?.Channel;
			var memberVoiceChannel = ctx.Member.VoiceState?.Channel;

			//if (botVoiceChannel == null && memberVoiceChannel != null)
			//{
			//    this.connection = await this.node.ConnectAsync(memberVoiceChannel);
			//}
			//else if (botVoiceChannel != null)
			//    this.connection = this.node.GetConnection(ctx.Guild);
			//else
			//    this.connection = await this.node.ConnectAsync(memberVoiceChannel);

			//if (this.connection != null && !this.connection.IsConnected)
			//{
			//    var objRef = this.node.GetType().GetProperty("ConnectedGuilds",
			//        BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this.node);

			//    ((ConcurrentDictionary<ulong, LavalinkGuildConnection>)objRef)
			//        .TryRemove(ctx.Guild.Id, out var _);

			//    this.connection = await this.node.ConnectAsync(memberVoiceChannel);
			//}

			if (this.connection == null || !this.connection.IsConnected)
			{
				this.connection = this.node.GetConnection(ctx.Guild);

				if (this.connection != null)
					this.connection.PlaybackFinished += this.NotifyTrackFinished;
			}

			if (this.connection == null || !this.connection.IsConnected)
			{
				if (botVoiceChannel == null || (botVoiceChannel != null && memberVoiceChannel == botVoiceChannel))
				{
					var objRef = this.node.GetType().GetProperty("ConnectedGuilds",
						BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this.node);

					((ConcurrentDictionary<ulong, LavalinkGuildConnection>)objRef)
						.TryRemove(ctx.Guild.Id, out var _);

					this.connection = await this.node.ConnectAsync(memberVoiceChannel);
					this.connection.PlaybackFinished += this.NotifyTrackFinished;
				}
			}
		}
	}
}
