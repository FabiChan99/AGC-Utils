using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniParser;
using IniParser.Model;
using Npgsql;

namespace AGC_Management.Services.DatabaseHandler
{
    public static class DatabaseService
    {
        private static NpgsqlConnection dbConnection;

        public static void OpenConnection()
        {
            try
            {
                
                string DbHost = GlobalProperties.ConfigIni["MainConfig"]["Database_Host"];
                string DbUser = GlobalProperties.ConfigIni["MainConfig"]["Database_User"];
                string DbPass = GlobalProperties.ConfigIni["MainConfig"]["Database_Password"];
                string DbName = GlobalProperties.ConfigIni["MainConfig"]["Database"];
                dbConnection = new NpgsqlConnection($"Host={DbHost};Username={DbUser};Password={DbPass};Database={DbName}");
                dbConnection.Open();
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

        // Change DBContent
        public static void ExecuteCommand(string sql)
        {
            try
            {
                using (var cmd = new NpgsqlCommand(sql, dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }
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
}
