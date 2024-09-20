using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace Store_Logs
{
	public class Db
	{
		[JsonPropertyName("Host")]
		public string Host { get; set; } = "";

		[JsonPropertyName("Port")]
		public int Port { get; set; } = 3306;

		[JsonPropertyName("User")]
		public string User { get; set; } = "";

		[JsonPropertyName("Password")]
		public string Password { get; set; } = "";

		[JsonPropertyName("Name")]
		public string Name { get; set; } = "";
	}

	public class Config : BasePluginConfig
	{
		[JsonPropertyName("ConfigVersion")] public override int Version { get; set; } = 1;

		[JsonPropertyName("Database")]
		public Db Database { get; set; } = new Db();

		[JsonPropertyName("DiscordWebhook")]
		public string DiscordWebhook { get; set; } = "";
	}
}
