#pragma warning disable CS1998 

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
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
        private TrackInfo state;
        private LavalinkConfiguration config;
        private LanaBot bot;

        public MusicModule(LanaBot bot)
        {
            this.bot = bot;
            this.tracks = new ConcurrentQueue<TrackInfo>();
            this.state = default;
            this.bot.Lavalink.NodeDisconnected += this.NotifyNodeDisconnected;
            this.UpdateConfiguration();
        }

        public void UpdateConfiguration()
            => this.config = new LavalinkConfiguration(this.bot.Configuration.Lavalink.Build());

        [Command, RequireVoiceChannel, RequireSameVoiceChannel, Priority(0)]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            var response = await this.node.Rest.GetTracksAsync(search, LavalinkSearchType.Youtube);

            if (response.Tracks?.Count() == 0)
            {
                await ctx.RespondAsync($"{ctx.User.Mention} :x: Nenhum resultado encontrado para `{Formatter.Sanitize(search)}`");
                return;
            }

            var selector = new TrackSelector(ctx, response.Tracks, search);
            var result = await selector.SelectAsync();

            if (result.TimedOut)
            {
                await ctx.RespondAsync($"{ctx.User.Mention} :x: Tempo limite esgotado.");
                return;
            }

            var code = await this.PlayTrackAsync(result.Info);
            var data = result.Info.Track;
            var message = string.Empty;
            var pos = this.tracks.Count + 1;

            if (code == 0)
                message = $"{ctx.User.Mention} :notes: Tocando agora **{Formatter.Sanitize(data.Title)}** `[{Formatter.Sanitize(data.Length.Format())}]`";
            else if (code == 1)
                message = $"{ctx.User.Mention} :headphones: Enfileirado [`#{pos}`] **{Formatter.Sanitize(data.Title)}** `[{Formatter.Sanitize(data.Length.Format())}]`";

            await ctx.RespondAsync(message);
        }

        [Command, RequireVoiceChannel, RequireSameVoiceChannel, Priority(1)]
        public async Task Play(CommandContext ctx, Uri url)
        {

        }

        async Task<int> PlayTrackAsync(TrackInfo track)
        {
            if (this.connection == null)
                return -1;

            if (this.state != null)
            {
                this.tracks.Enqueue(track);
                return 1;
            }
            else
            {
                this.state = track;
                await this.connection.PlayAsync(this.state.Track);
                return 0;
            }
        }

        Task NotifyNodeDisconnected(NodeDisconnectedEventArgs e)
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

            var botVoiceChannel = ctx.Guild.CurrentMember.VoiceState?.Channel;
            var memberVoiceChannel = ctx.Member.VoiceState?.Channel;

            if (botVoiceChannel == null && memberVoiceChannel != null)
            {
                this.connection = await this.node.ConnectAsync(memberVoiceChannel);
            }
            else if (botVoiceChannel != null)
                this.connection = this.node.GetConnection(ctx.Guild);
            else
                this.connection = await this.node.ConnectAsync(memberVoiceChannel);

            if (this.connection != null && !this.connection.IsConnected)
            {
                var objRef = this.node.GetType().GetProperty("ConnectedGuilds",
                    BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this.node);

                ((ConcurrentDictionary<ulong, LavalinkGuildConnection>)objRef)
                    .TryRemove(ctx.Guild.Id, out var _);

                this.connection = await this.node.ConnectAsync(memberVoiceChannel);
            }
        }
    }
}
