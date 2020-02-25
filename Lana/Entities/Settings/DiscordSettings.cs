using System;
using DSharpPlus;
using Newtonsoft.Json;

namespace Lana.Entities.Settings
{
    public class DiscordSettings
    {
        [JsonIgnore]
        private string secret;

        [JsonProperty]
        public string Token
        {
            get => string.Empty;
            set => this.secret = value;
        }

        [JsonIgnore]
        public bool HasInvalidToken => string.IsNullOrEmpty(this.secret);

        [JsonProperty]
        public GatewayCompressionLevel GatewayCompressionLevel { get; private set; } = GatewayCompressionLevel.Stream;

        [JsonProperty]
        public bool AutoReconnect { get; private set; } = true;

        [JsonProperty]
        public bool ReconnectIndefinitely { get; private set; } = false;

        [JsonProperty]
        public TimeSpan HttpTimeout { get; private set; } = TimeSpan.FromSeconds(30d);

        public DiscordConfiguration Build()
        {
            return new DiscordConfiguration
            {
                Token = this.secret,
                TokenType = TokenType.Bot,
                AutoReconnect = this.AutoReconnect,
                GatewayCompressionLevel = this.GatewayCompressionLevel,
                HttpTimeout = this.HttpTimeout,
                ReconnectIndefinitely = this.ReconnectIndefinitely,

#if DEBUG
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true
#endif
            };
        }
    }
}