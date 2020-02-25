using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Lana.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireBotDeveloperAttribute : CheckBaseAttribute
    {
        static readonly ulong[] Developers = new[]
        {
            163324170556538880UL,
            143466929615667201UL
        };

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (help)
                return Task.FromResult(true);

            return Task.FromResult(Developers.Any(x => ctx.User.Id == x));
        }
    }
}
