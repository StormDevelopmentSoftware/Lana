﻿using System;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Newtonsoft.Json;

namespace Lana.Entities.Settings
{
	public class LavalinkSettings
	{
		[JsonIgnore]
		private string secret = "youshallnotpass";

		[JsonProperty]
		public ConnectionEndpoint RestEndpoint { get; private set; } = new ConnectionEndpoint("127.0.0.1", 2333);

		[JsonProperty]
		public ConnectionEndpoint SocketEndpoint { get; private set; } = new ConnectionEndpoint("127.0.0.1", 2333);

		[JsonProperty]
		public string Password
		{
			get => string.Empty;
			protected set => this.secret = value;
		}

		[JsonProperty]
		public TimeSpan ResumeTimeout { get; private set; } = TimeSpan.FromMinutes(3d);

		public LavalinkConfiguration Build()
		{
			return new LavalinkConfiguration
			{
				Password = this.secret,
				RestEndpoint = this.RestEndpoint,
				SocketEndpoint = this.SocketEndpoint,
			};
		}
	}
}
