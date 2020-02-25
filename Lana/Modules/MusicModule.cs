﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
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
            {
                this.connection = await this.node.ConnectAsync(memberVoiceChannel);
            }
            else if (botVoiceChannel != null)
                this.connection = this.node.GetConnection(ctx.Guild);
            else
                this.connection = await this.node.ConnectAsync(memberVoiceChannel);

            if(this.connection != null && !this.connection.IsConnected)
            {
                var objRef = this.node.GetType().GetProperty("ConnectedGuilds",
                    BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this.node);

                ((ConcurrentDictionary<ulong, LavalinkGuildConnection>)objRef)
                    .TryRemove(ctx.Guild.Id, out var _);

                this.connection = await this.node.ConnectAsync(memberVoiceChannel);
            }
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
