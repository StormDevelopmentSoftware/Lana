using System;
using System.Text;
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
            Console.OutputEncoding = Encoding.UTF8;

            var config = await LanaConfiguration.LoadAsync();

            if (config.Discord.HasInvalidToken)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[DISCORD] Token invalido está presente na configuração!");
                Console.ReadKey(true);
                return;
            }

            var bot = new LanaBot(config);
            await bot.InitializeAsync();

            while (!Cts.IsCancellationRequested)
                await Task.Delay(1);

            await bot.ShutdownAsync();
        }
    }
}
