using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Lana.Attributes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public sealed class RequireVoiceChannelAttribute : CheckBaseAttribute
	{
		public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		{
			if (help)
				return Task.FromResult(true);

			var vschn = ctx.Member.VoiceState?.Channel;
			return Task.FromResult(vschn != null);
		}
	}
}
