using MySqlConnector;

namespace Store_Logs.Database;

public class Database(string dbConnectionString)
{
	public MySqlConnection GetConnection()
	{
		try
		{
			var connection = new MySqlConnection(dbConnectionString);
			connection.Open();
			return connection;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[Store Logs] Unable to connect to database: {ex.Message}");
			throw;
		}
	}

	public async Task<MySqlConnection> GetConnectionAsync()
	{
		try
		{
			var connection = new MySqlConnection(dbConnectionString);
			await connection.OpenAsync();
			return connection;
		}
		catch (Exception ex)
		{
			Console.WriteLine($"[Store Logs] Unable to connect to database: {ex.Message}");
			throw;
		}
	}

	public bool CheckDatabaseConnection()
	{
		using var connection = GetConnection();

		try
		{
			return connection.Ping();
		}
		catch
		{
			return false;
		}
	}
}
