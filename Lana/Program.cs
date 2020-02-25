using System;
using System.Threading;
using System.Threading.Tasks;
using Lana.Entities;

namespace Lana
{
    class Program
    {
        static readonly CancellationTokenSource Cts
            = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            var config = await LanaConfiguration.LoadAsync();

            if (config.Discord.HasInvalidToken)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[CONFIG] Invalid token was found!");
                Console.ReadKey(true);
                return;
            }

            var bot = new LanaBot(config);

            while (!Cts.IsCancellationRequested)
                await Task.Delay(1);
        }
    }
}
