using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Lana.Entities.Music
{
	public class VoteSkip : IDisposable
	{
		private CommandContext context;
		private DiscordMessage message;
		private DiscordClient client;
		private TimeSpan timeout;
		private HashSet<VoteSkipRecord> votes;
		TaskCompletionSource<VoteSkipResult> tsc;
		private volatile bool disposed;

		public VoteSkipResult Result { get; private set; }

		static readonly DiscordEmoji ThumbsUp;
		static readonly DiscordEmoji ThumbsDown;

		static VoteSkip()
		{
			ThumbsUp = DSharpPlusExtensions.LookupEmoji(":thumbsup:");
			ThumbsDown = DSharpPlusExtensions.LookupEmoji(":thumbsdown:");
		}

		public VoteSkip(CommandContext ctx, TimeSpan timeout)
		{
			this.Result = VoteSkipResult.Failed;
			this.timeout = timeout;
			this.context = ctx;
			this.client = this.context.Client;
			this.votes = new HashSet<VoteSkipRecord>();

			this.tsc = new TaskCompletionSource<VoteSkipResult>();
		}

		public async Task<VoteSkipResult> InitializeAsync()
		{
			this.message = await this.context.RespondAsync("Fulando quer pular a música... [TESTE]");
			await message.CreateReactionAsync(ThumbsUp);
			await message.CreateReactionAsync(ThumbsDown);
			this.client.MessageReactionAdded += this.OnReactionCreated;
			
			var cts = new CancellationTokenSource(this.timeout);
			cts.Token.Register(() => this.tsc.TrySetResult(VoteSkipResult.Failed));
			
			return await tsc.Task;
		}

		public bool Compute(VoteSkipRecord item)
		{
			lock (this.votes)
			{
				if (this.votes.Contains(item))
					return false;

				this.votes.Add(item);
				return true;
			}
		}

		public IEnumerable<VoteSkipRecord> GetVotes()
		{
			VoteSkipRecord[] votes;

			lock (this.votes)
				votes = this.votes.ToArray();

			return votes;
		}

		/// <summary>
		/// Evento disparado quando um usuário reage ao voto de pular a música atual.
		/// </summary>
		async Task OnReactionCreated(MessageReactionAddEventArgs e)
		{
			await Task.Yield();

			if (e.Message == null)
				return;

			if (e.Message != this.message)
				return;

			if (e.User.IsBot || e.User.IsCurrent)
				return;

			if (e.Emoji != ThumbsUp && e.Emoji != ThumbsDown)
				return;

			// TODO aceitar voto apenas de membros no canal de voz.

			var vote = new VoteSkipRecord(e.User, e.Emoji == ThumbsUp);

			if(this.Compute(vote))
				_ = Task.Factory.StartNew(async () => await this.OnVoteCollected(vote));
		}

		async Task OnVoteCollected(VoteSkipRecord vote)
		{
			// verifica se a quantidade de votos ou sla um fator 0.6 >= 
			// use .GetVotes() ao inves da váriável this.votes, por causa da concorrência de threads.
			// chame .Dispose() para remover o handler de reação.
			// use tsc.TrySetResult para definir o resultado final da votação.

			this.message = await this.message.ModifyAsync(content: message.Content + "\n"
				+ $"{vote.User.Username}#{vote.User.Discriminator}: {(vote.State ? ThumbsUp : ThumbsDown)}");

			tsc.TrySetResult(vote.State ? VoteSkipResult.Success : VoteSkipResult.Failed);
		}

		public void Dispose()
		{
			if (this.disposed)
				return;

			this.disposed = true;

			if (this.client != null)
				this.client.MessageReactionAdded -= this.OnReactionCreated;
		}
	}
}
