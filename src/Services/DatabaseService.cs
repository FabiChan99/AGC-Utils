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
            var parser = new FileIniDataParser();
            IniData iniData = parser.ReadFile("config.ini");
            string DbHost = iniData["MainConfig"]["Database_Host"];
            string DbUser = iniData["MainConfig"]["Database_User"];
            string DbPass = iniData["MainConfig"]["Database_Password"];
            string DbName = iniData["MainConfig"]["Database"];
            dbConnection = new NpgsqlConnection($"Host={DbHost};Username={DbUser};Password={DbPass};Database={DbName}");
            dbConnection.Open();
        }

        public static void CloseConnection() 
        {
            dbConnection.Close();
            dbConnection.Dispose();
        }
        
        //  Change DBContent
        public static void ExecuteCommand(string sql)
        {
            using (var cmd = new NpgsqlCommand(sql, dbConnection))
            {
                cmd.ExecuteNonQuery();
            }
        }
        //  Read DBContent
        public static NpgsqlDataReader ExecuteQuery(string sql)
        {
            using (var cmd = new NpgsqlCommand(sql, dbConnection))
            {
                return cmd.ExecuteReader();
            }
        }
    }
}
