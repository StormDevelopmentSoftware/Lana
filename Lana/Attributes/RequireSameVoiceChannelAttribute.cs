using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Lana.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RequireSameVoiceChannelAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (help)
                return Task.FromResult(true);

            var memberVoiceChannel = ctx.Member.VoiceState?.Channel;
            var botVoiceChannel = ctx.Guild.CurrentMember.VoiceState?.Channel;

            if (botVoiceChannel == null)
                return Task.FromResult(true);
            else
            {
                if (memberVoiceChannel == null)
                    return Task.FromResult(false);

                return Task.FromResult(memberVoiceChannel == botVoiceChannel);
            }
        }
    }
}
