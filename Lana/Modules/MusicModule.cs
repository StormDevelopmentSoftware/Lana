using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
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
        private TrackInfo currentTrack;
        private LavalinkConfiguration config;
        private LanaBot bot;

        public MusicModule(LanaBot bot)
        {
            this.bot = bot;
            this.bot.Lavalink.NodeDisconnected += this.ProcessNodeDisconnected;
            this.config = new LavalinkConfiguration(this.bot.Configuration.Lavalink.Build());
        }

        Task ProcessNodeDisconnected(NodeDisconnectedEventArgs e)
        {
            if (e.LavalinkNode == this.node)
                this.connection = default;

            return Task.CompletedTask;
        }

        public override async Task BeforeExecutionAsync(CommandContext ctx)
        {
            var lavalink = ctx.Client.GetLavalink();

            if (this.node == null || !this.node.IsConnected)
                this.node = lavalink.GetNodeConnection(this.bot.Configuration.Lavalink.RestEndpoint);

            if (this.node == null || !this.node.IsConnected)
                this.node = await lavalink.ConnectAsync(this.config);

            var botVoiceChannel = ctx.Guild.CurrentMember.VoiceState?.Channel;
            var memberVoiceChannel = ctx.Member.VoiceState?.Channel;

            if (botVoiceChannel == null && memberVoiceChannel != null)
                this.connection = await this.node.ConnectAsync(memberVoiceChannel);
            else if (botVoiceChannel != null)
                this.connection = this.node.GetConnection(ctx.Guild);
            else
                this.connection = await this.node.ConnectAsync(memberVoiceChannel);
        }

        [Command, RequireBotDeveloper]
        public async Task Kick(CommandContext ctx)
        {
            if (ctx.Guild.CurrentMember.VoiceState?.Channel != null)
            {
                await ctx.Guild.CurrentMember.ModifyAsync(x => x.VoiceChannel = null);
                await ctx.RespondAsync($"Bot saiu do canal de voz.");
            }
        }

        /*[Command, RequireVoiceChannel, RequireSameVoiceChannel, Priority(0)]
        public async Task Play(CommandContext ctx, string search)
        {

        }*/

        [Command, RequireVoiceChannel, RequireSameVoiceChannel, Priority(1)]
        public async Task Play(CommandContext ctx, Uri url)
        {
            var loadResult = await this.node.Rest.GetTracksAsync(url);

            await ctx.RespondAsync($"status: {loadResult.LoadResultType}");

            if (!loadResult.Tracks.Any())
            {
                await ctx.RespondAsync($"{ctx.User.Mention} :x:");
                return;
            }

            var track = loadResult.Tracks.FirstOrDefault(x => x != null);

            if (track == null)
            {
                await ctx.RespondAsync(":x: Esta nula!");
                return;
            }

            await this.connection.PlayAsync(track);
            await ctx.RespondAsync("Tocando musica (ou deveria kkkkkk)");
        }
    }
}
