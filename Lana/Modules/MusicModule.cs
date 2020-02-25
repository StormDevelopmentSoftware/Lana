using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Lavalink;
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

        public MusicModule()
        {

        }

        public override async Task BeforeExecutionAsync(CommandContext ctx)
        {
            var lavalink = ctx.Client.GetLavalink();

            if (this.node == null || !this.node.IsConnected)
                this.node = await lavalink.ConnectAsync(this.config);

            var botVoiceChannel = ctx.Guild.CurrentMember.VoiceState?.Channel;
            var memberVoiceChannel = ctx.Member.VoiceState?.Channel;

            if (botVoiceChannel == null && memberVoiceChannel != null)
                this.connection = await this.node.ConnectAsync(memberVoiceChannel);
        }

        [Command, RequireVoiceChannel, RequireSameVoiceChannel, Priority(0)]
        public async Task Play(CommandContext ctx, string search)
        {

        }

        [Command, RequireVoiceChannel, RequireSameVoiceChannel, Priority(1)]
        public async Task Play(CommandContext ctx, Uri url)
        {

        }
    }
}
