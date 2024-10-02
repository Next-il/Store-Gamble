using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Discord.Webhook;
using MySqlConnector;
using StoreApi;
using Dapper;
using Discord;

namespace Store_Logs;

public class Store_Logs : BasePlugin, IPluginConfig<Config>
{
	public override string ModuleName => "Store Module [Logs]";
	public override string ModuleVersion => "1.0.0";
	public override string ModuleAuthor => "ShiNxz";

	public Config Config { get; set; } = new();
	private string _dbConnectionString = string.Empty;
	private static Database.Database? _database;
	public static DiscordWebhookClient? DiscordWebhookClientLog { get; set; }

	public IStoreApi? StoreApi { get; set; }

	public void OnConfigParsed(Config config)
	{
		Console.WriteLine("[Store Logs] Config parsed!");

		if (config.Database.Host.Length < 1 || config.Database.Name.Length < 1 || config.Database.User.Length < 1)
		{
			throw new Exception("[Store Logs] You need to setup Database credentials in config!");
		}

		MySqlConnectionStringBuilder builder = new()
		{
			Server = config.Database.Host,
			Database = config.Database.Name,
			UserID = config.Database.User,
			Password = config.Database.Password,
			Port = (uint)config.Database.Port,
			Pooling = true,
			MinimumPoolSize = 0,
			MaximumPoolSize = 640,
		};

		_dbConnectionString = builder.ConnectionString;
		_database = new Database.Database(_dbConnectionString);

		if (!_database.CheckDatabaseConnection())
		{
			Console.WriteLine("[Store Logs] Unable connect to database!");
			Unload(false);
			return;
		}

		Task.Run(async () =>
		{
			try
			{
				using MySqlConnection connection = await _database.GetConnectionAsync();
				using MySqlTransaction transaction = await connection.BeginTransactionAsync();

				try
				{
					string SqlQuery = "CREATE TABLE IF NOT EXISTS `store_logs` (" +
										"`id` INT NOT NULL AUTO_INCREMENT PRIMARY KEY," +
										"`player_steamid` VARCHAR(64)," +
										"`player_name` VARCHAR(128)," +
										"`amount` INT NOT NULL," +
										"`new_amount` INT NOT NULL," +
										"`reason` TEXT NULL," +
										"`created` TIMESTAMP NOT NULL" +
									  ") ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;";

					await connection.QueryAsync(SqlQuery, transaction: transaction);
					await transaction.CommitAsync();

					Console.WriteLine("[Store Logs] Connected to database!");
				}
				catch (Exception)
				{
					await transaction.RollbackAsync();
					throw;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Store Logs] Unable to connect to the database: {ex.Message}");
				throw;
			}
		});

		Config = config;

		if (!string.IsNullOrEmpty(Config.DiscordWebhook))
			DiscordWebhookClientLog = new DiscordWebhookClient(Config.DiscordWebhook);
	}

	public override void OnAllPluginsLoaded(bool hotReload)
	{
		Console.WriteLine("Store Module [Logs] loaded.");
		Console.WriteLine("Listening to StoreApi events...");
		Console.WriteLine(IStoreApi.Capability.Get() != null ? "StoreApi located." : "StoreApi could not be located.");
		StoreApi = IStoreApi.Capability.Get() ?? throw new Exception("StoreApi could not be located.");

		StoreApi.OnGivePlayerCredits += ((CCSPlayerController player, int credits, string? reason) args) =>
		{
			var (player, credits, reason) = args;

			Console.WriteLine($"Player {player.PlayerName} received {credits} credits. Reason: {reason}");

			int newAmount = StoreApi.GetPlayerCredits(player) + credits;

			if (_database != null)
			{
				SaveDataToDatabase(player.SteamID.ToString(), player.PlayerName, credits, newAmount, reason);
			}

			if (DiscordWebhookClientLog != null)
			{
				string title = credits < 0 ? "Lost" : "Received";

				// Remove the minus sign from the credits
				if (credits < 0)
					credits = Math.Abs(credits);

				DiscordLog.DiscordEmbed(
					client: DiscordWebhookClientLog,
					title: $"{player.PlayerName}",
					description: $"➜ SteamId: [{player.SteamID}](http://steamcommunity.com/profiles/{player.SteamID})\n ➜ {title} {credits} credits.\n➜ Reason: {reason}.\n➜ New Amount: {newAmount}.",
					color: credits < 0 ? Color.DarkRed : Color.DarkGreen
				);
			}
		};
	}

	// Save data to database
	public static Task SaveDataToDatabase(string playerSteamId, string playerName, int amount, int newAmount, string? reason)
	{
		return Task.Run(async () =>
		{
			try
			{
				using MySqlConnection connection = await _database!.GetConnectionAsync();
				using MySqlTransaction transaction = await connection.BeginTransactionAsync();

				try
				{
					string SqlQuery = "INSERT INTO `store_logs` (`player_steamid`, `player_name`, `amount`, `new_amount`, `reason`, `created`) VALUES (@player_steamid, @player_name, @amount, @new_amount, @reason, NOW());";

					await connection.ExecuteAsync(SqlQuery, new
					{
						player_steamid = playerSteamId,
						player_name = playerName,
						amount = amount,
						new_amount = newAmount,
						reason = reason
					}, transaction: transaction);

					await transaction.CommitAsync();
				}
				catch (Exception)
				{
					await transaction.RollbackAsync();
					throw;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[Store Logs] Unable to save data to database: {ex.Message}");
				throw;
			}
		});
	}
}
