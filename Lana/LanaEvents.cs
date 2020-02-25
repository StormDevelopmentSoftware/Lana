// File LanaEvents.cs created by Animadoria (me@animadoria.cf) at 2/25/2020 7:17 PM for the Lana bot.
using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Lana
{
    public class LanaEvents
    {
        private readonly ulong guildID = 681496938092429357;
        private readonly ulong logChannelID = 681940088045043717;
        private DiscordChannel logChannel;

        public async Task InitializeEventsAsync()
        {
            logChannel = (await LanaBot.Instance.Discord.GetGuildAsync(guildID)).GetChannel(logChannelID);
            LanaBot.Instance.Discord.MessageDeleted += Discord_MessageDeleted;
            LanaBot.Instance.Discord.MessageUpdated += Discord_MessageUpdated;
        }



        private Task Discord_MessageUpdated(MessageUpdateEventArgs e)
        {
            if (e.Author == e.Client.CurrentUser)
                return Task.CompletedTask;

            logChannel.SendMessageAsync(embed: new DiscordEmbedBuilder()
                .WithTitle("Mensagem editada")
                .WithColor(DiscordColor.Yellow)
                .WithDescription($"{e.Author.Username}#{e.Author.Discriminator} (ID {e.Author.Id}) mudou a sua mensagem, de {Formatter.BlockCode(Formatter.Sanitize(e.MessageBefore.Content))} para {Formatter.BlockCode(Formatter.Sanitize(e.Message.Content))}")
                .WithTimestamp(DateTime.Now)
                .WithFooter($"{e.Author.Username}#{e.Author.Discriminator}", e.Author.AvatarUrl));

            return Task.CompletedTask;
        }

        private Task Discord_MessageDeleted(MessageDeleteEventArgs e)
        {
            if (e.Message.Author == e.Client.CurrentUser)
                return Task.CompletedTask;

            logChannel.SendMessageAsync(embed: new DiscordEmbedBuilder()
                .WithTitle("Mensagem deletada")
                .WithColor(DiscordColor.Red)
                .WithDescription($"{e.Message.Author.Username}#{e.Message.Author.Discriminator} (ID {e.Message.Author.Id}) apagou a sua mensagem, {Formatter.BlockCode(Formatter.Sanitize(e.Message.Content))}")
                .WithTimestamp(DateTime.Now)
                .WithFooter($"{e.Message.Author.Username}#{e.Message.Author.Discriminator}", e.Message.Author.AvatarUrl));

            return Task.CompletedTask;
        }
    }
}
