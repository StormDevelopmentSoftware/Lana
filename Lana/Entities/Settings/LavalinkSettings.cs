using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Net;
using Newtonsoft.Json;

namespace Lana.Entities.Settings
{
    public class LavalinkSettings
    {
        [JsonProperty]
        public ConnectionEndpoint RestEndpoint { get; private set; } = new ConnectionEndpoint("127.0.0.1", 2333);

        [JsonProperty]
        public ConnectionEndpoint SocketEndpoint { get; private set; } = new ConnectionEndpoint("127.0.0.1", 2333);

        public string Password { get; private set; } = "youshallnotpass";

        [JsonProperty]
        public string ResumeKey { get;  private set; } = "lanabot-lavalink";

        [JsonProperty]
        public int ResumeTimeout { get; private set; } = 60;
    }
}
