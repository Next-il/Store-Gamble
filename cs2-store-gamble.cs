using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using StoreApi;
using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace Store_Gamble;

public class Store_GambleConfig : BasePluginConfig
{
	[JsonPropertyName("enable_gamble")]
	public bool EnableGamble { get; set; } = true;

	[JsonPropertyName("min_credits")]
	public int MinCredits { get; set; } = 0;

	[JsonPropertyName("max_credits")]
	public int MaxCredits { get; set; } = 1000;

	[JsonPropertyName("gamble_commands")]
	public List<string> GambleCommands { get; set; } = ["gamble", "bet", "g"];

	[JsonPropertyName("win_chance")]
	public int WinChance { get; set; } = 50;

	[JsonPropertyName("cooldown")]
	public int Cooldown { get; set; } = 10;
}

public class Store_Gamble : BasePlugin, IPluginConfig<Store_GambleConfig>
{
	public override string ModuleName => "Store Module [Gamble]";
	public override string ModuleVersion => "1.0.0";
	public override string ModuleAuthor => "ShiNxz (Using Nathy's Code)";

	public IStoreApi? StoreApi { get; set; }
	public Dictionary<string, Dictionary<CCSPlayerController, int>> GlobalGamble { get; set; } = [];
	public Random Random { get; set; } = new();
	public Store_GambleConfig Config { get; set; } = new Store_GambleConfig();

	private readonly ConcurrentDictionary<string, DateTime> playerLastGamblesTimes = new();

	public override void OnAllPluginsLoaded(bool hotReload)
	{
		StoreApi = IStoreApi.Capability.Get() ?? throw new Exception("StoreApi could not be located.");

		CreateCommands();
	}

	private void CreateCommands()
	{
		foreach (string cmd in Config.GambleCommands)
		{
			AddCommand($"css_{cmd}", "Gamble Bet", Command_Gamble);
		}
	}

	public void OnConfigParsed(Store_GambleConfig config)
	{
		config.MinCredits = Math.Max(0, config.MinCredits);
		config.MaxCredits = Math.Max(config.MinCredits + 1, config.MaxCredits);

		config.WinChance = Math.Clamp(config.WinChance, 1, 100);

		Config = config;
	}

	public void Command_Gamble(CCSPlayerController? player, CommandInfo info)
	{
		if (player == null)
		{
			return;
		}

		if (StoreApi == null)
		{
			throw new Exception("StoreApi could not be located.");
		}

		if (playerLastGamblesTimes.TryGetValue(player.SteamID.ToString(), out var lastChallengeTime))
		{
			var cooldownRemaining = (DateTime.Now - lastChallengeTime).TotalSeconds;
			if (cooldownRemaining < Config.Cooldown)
			{
				var secondsRemaining = (int)(Config.Cooldown - cooldownRemaining);
				info.ReplyToCommand(Localizer["Prefix"] + Localizer["Cooldown", secondsRemaining]);
				return;
			}
		}

		if (!Config.EnableGamble)
		{
			info.ReplyToCommand(Localizer["Prefix"] + Localizer["Gamble feature is disabled"]);
			return;
		}

		if (!int.TryParse(info.GetArg(1), out int credits))
		{
			info.ReplyToCommand(Localizer["Prefix"] + Localizer["Must be an integer"]);
			return;
		}

		if (StoreApi.GetPlayerCredits(player) < credits)
		{
			info.ReplyToCommand(Localizer["Prefix"] + Localizer["No enough credits"]);
			return;
		}

		if (credits < Config.MinCredits)
		{
			info.ReplyToCommand(Localizer["Prefix"] + Localizer["Min gamble", Config.MinCredits]);
			return;
		}

		if (credits > Config.MaxCredits)
		{
			info.ReplyToCommand(Localizer["Prefix"] + Localizer["Max gamble", Config.MaxCredits]);
			return;
		}

		playerLastGamblesTimes[player.SteamID.ToString()] = DateTime.Now;

		// Start the gamble
		var win = Random.Next(1, 101) <= Config.WinChance;
		var winCredits = win ? credits : -credits;

		StoreApi.GivePlayerCredits(player, winCredits);

		Server.PrintToChatAll(Localizer["Prefix"] + Localizer[win ? "Win" : "Lose", player.PlayerName, credits]);
	}
}
