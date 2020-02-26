using System;
using System.IO;
using System.Threading.Tasks;
using Lana.Entities.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Lana.Entities
{
	public class LanaConfiguration
	{
		[JsonProperty]
		public DiscordSettings Discord { get; private set; } = new DiscordSettings();

		[JsonProperty]
		public LavalinkSettings Lavalink { get; private set; } = new LavalinkSettings();

		public async Task ReloadAsync()
		{
			var result = await LoadAsync();
			this.Discord = result.Discord;
			this.Lavalink = result.Lavalink;
		}

		public static Task<LanaConfiguration> LoadAsync()
		{
			var settings = new JsonSerializerSettings
			{
				ContractResolver = new DefaultContractResolver
				{
					NamingStrategy = new SnakeCaseNamingStrategy()
				}
			};

			var result = new LanaConfiguration();

			var file = new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), "Config.json"));

			if (!file.Exists)
			{
				using (var sw = file.CreateText())
				{
					sw.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented, settings));
					sw.Flush();
				}
			}
			else
			{
				using (var sr = file.OpenText())
				{
					try
					{
						var json = sr.ReadToEnd();
						result = JsonConvert.DeserializeObject<LanaConfiguration>(json, settings);
					}
					catch (Exception ex)
					{
						Console.ForegroundColor = ConsoleColor.Yellow;
						Console.Write("[LANA] Inicialização da configuração falhou! ");
						Console.ForegroundColor = ConsoleColor.Red;
						Console.Write(ex);
						Console.ResetColor();
					}
				}
			}

			return Task.FromResult(result);
		}
	}
}