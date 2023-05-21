using Npgsql;
using System.Data;

namespace AGC_Management.Services.DatabaseHandler;

public static class DatabaseService
{
    private static NpgsqlConnection dbConnection;

    public static void OpenConnection()
    {
        try
        {
            var DbHost = GlobalProperties.ConfigIni["DatabaseCfg"]["Database_Host"];
            var DbUser = GlobalProperties.ConfigIni["DatabaseCfg"]["Database_User"];
            var DbPass = GlobalProperties.ConfigIni["DatabaseCfg"]["Database_Password"];
            var DbName = GlobalProperties.ConfigIni["DatabaseCfg"]["Database"];
            dbConnection = new NpgsqlConnection($"Host={DbHost};Username={DbUser};Password={DbPass};Database={DbName}");
            try
            {
                dbConnection.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while opening the database connection: " + ex.Message +
                                  "\nFunctionality will be restricted and the Program can be Unstable, Continue at own risk!\nPress any key to Continue");
                Console.ReadKey();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while opening the database connection: " + ex.Message);
            throw;
        }
    }

    public static void CloseConnection()
    {
        try
        {
            dbConnection.Close();
            dbConnection.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while closing the database connection: " + ex.Message);
            throw;
        }
    }

    public static bool IsConnected()
    {
        try
        {
            if (dbConnection.State == ConnectionState.Open)
                return true;
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // Change DBContent
    public static void ExecuteCommand(string sql)
    {
        try
        {
            using var cmd = new NpgsqlCommand(sql, dbConnection);
            cmd.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while executing the database command: " + ex.Message);
            throw;
        }
    }

    // Read DBContent
    public static NpgsqlDataReader ExecuteQuery(string sql)
    {
        try
        {
            using (var cmd = new NpgsqlCommand(sql, dbConnection))
            {
                return cmd.ExecuteReader();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while executing the database query: " + ex.Message);
            throw;
        }
    }
}