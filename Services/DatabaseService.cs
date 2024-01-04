#region

using System.Data;

#endregion

namespace AGC_Management.Services;

public static class DatabaseService
{
    private static NpgsqlConnection? dbConnection;

    public static void OpenConnection()
    {
        try
        {
            var dbConfigSection = GlobalProperties.DebugMode ? "DatabaseCfgDBG" : "DatabaseCfg";
            var DbHost = BotConfig.GetConfig()[dbConfigSection]["Database_Host"];
            var DbUser = BotConfig.GetConfig()[dbConfigSection]["Database_User"];
            var DbPass = BotConfig.GetConfig()[dbConfigSection]["Database_Password"];
            var DbName = BotConfig.GetConfig()[dbConfigSection]["Database"];

            dbConnection = new NpgsqlConnection($"Host={DbHost};Username={DbUser};Password={DbPass};Database={DbName}");
            try
            {
                if (dbConnection.State != ConnectionState.Open)
                    dbConnection.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while opening the database connection: " + ex.Message +
                                  "\nFunctionality will be restricted and the Program can be Unstable. Continue at your own risk!\nPress any key to continue");
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

    public static string GetConnectionString()
    {
        var dbConfigSection = GlobalProperties.DebugMode ? "DatabaseCfgDBG" : "DatabaseCfg";
        var DbHost = BotConfig.GetConfig()[dbConfigSection]["Database_Host"];
        var DbUser = BotConfig.GetConfig()[dbConfigSection]["Database_User"];
        var DbPass = BotConfig.GetConfig()[dbConfigSection]["Database_Password"];
        var DbName = BotConfig.GetConfig()[dbConfigSection]["Database"];
        return $"Host={DbHost};Username={DbUser};Password={DbPass};Database={DbName}";
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

    public static async Task InsertDataIntoTable(string tableName, Dictionary<string, object> columnValuePairs)
    {
        string insertQuery = $"INSERT INTO {tableName} ({string.Join(", ", columnValuePairs.Keys)}) " +
                             $"VALUES ({string.Join(", ", columnValuePairs.Keys.Select(k => $"@{k}"))})";

        await using (NpgsqlConnection connection = new(GetConnectionString()))
        {
            await connection.OpenAsync();

            await using (NpgsqlCommand command = new(insertQuery, connection))
            {
                foreach (var kvp in columnValuePairs)
                {
                    NpgsqlParameter parameter = new($"@{kvp.Key}", kvp.Value);
                    command.Parameters.Add(parameter);
                }

                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public static async Task<List<Dictionary<string, object>>> SelectDataFromTable(string tableName,
        List<string> columns, Dictionary<string, object> whereConditions)
    {
        string selectQuery;
        if (columns.Contains("*"))
        {
            selectQuery = $"SELECT * FROM \"{tableName}\"";
        }
        else
        {
            string columnNames = string.Join(", ", columns.Select(c => $"\"{c}\""));
            selectQuery = $"SELECT {columnNames} FROM \"{tableName}\"";
        }

        if (whereConditions != null && whereConditions.Count > 0)
        {
            string whereClause = string.Join(" AND ", whereConditions.Select(c => $"\"{c.Key}\" = @{c.Key}"));
            selectQuery += $" WHERE {whereClause}";
        }

        List<Dictionary<string, object>> results = new();

        await using (NpgsqlConnection connection = new(GetConnectionString()))
        {
            await connection.OpenAsync();

            await using (NpgsqlCommand command = new(selectQuery, connection))
            {
                if (whereConditions != null && whereConditions.Count > 0)
                    foreach (var condition in whereConditions)
                    {
                        NpgsqlParameter parameter = new($"@{condition.Key}", condition.Value);
                        command.Parameters.Add(parameter);
                    }

                await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Dictionary<string, object> row = new();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            string columnName = reader.GetName(i);
                            object columnValue = reader.GetValue(i);

                            row[columnName] = columnValue;
                        }

                        results.Add(row);
                    }
                }
            }
        }

        return results;
    }

    public static async Task<int> DeleteDataFromTable(string tableName,
        Dictionary<string, (object value, string comparisonOperator)> whereConditions, string logicalOperator = "AND")
    {
        string deleteQuery = $"DELETE FROM \"{tableName}\"";

        if (whereConditions != null && whereConditions.Count > 0)
        {
            string whereClause = string.Join($" {logicalOperator} ",
                whereConditions.Select(c => $"\"{c.Key}\" {c.Value.comparisonOperator} @{c.Key}"));
            deleteQuery += $" WHERE {whereClause}";
        }

        int rowsAffected;

        await using (NpgsqlConnection connection = new(GetConnectionString()))
        {
            await connection.OpenAsync();

            await using (NpgsqlCommand command = new(deleteQuery, connection))
            {
                if (whereConditions != null && whereConditions.Count > 0)
                    foreach (var condition in whereConditions)
                    {
                        NpgsqlParameter parameter = new($"@{condition.Key}", condition.Value.value);
                        command.Parameters.Add(parameter);
                    }

                rowsAffected = await command.ExecuteNonQueryAsync();
            }
        }

        return rowsAffected;
    }


    public static async Task InitializeAndUpdateDatabaseTables()
    {
        var dbstring = GetConnectionString();
        await using var conn = new NpgsqlConnection(dbstring);
        CurrentApplication.Logger.Information("Initializing database tables...");

        await conn.OpenAsync();

        var tableCommands = new Dictionary<string, string>
        {
            { "reasonmap", "CREATE TABLE IF NOT EXISTS reasonmap (key TEXT, text TEXT)" },
            { "banreasons", "CREATE TABLE IF NOT EXISTS banreasons (reason TEXT, custom_id VARCHAR)" },
            {
                "flags",
                "CREATE TABLE IF NOT EXISTS flags (userid BIGINT, punisherid BIGINT, datum BIGINT, description VARCHAR, caseid VARCHAR)"
            },
            {
                "tempvoice",
                "CREATE TABLE IF NOT EXISTS tempvoice (channelid BIGINT, ownerid BIGINT, lastedited BIGINT, laststatusedited BIGINT, channelmods VARCHAR)"
            },
            {
                "tempvoicesession",
                "CREATE TABLE IF NOT EXISTS tempvoicesession (userid BIGINT, channelname VARCHAR, channelbitrate INTEGER, channellimit INTEGER, blockedusers VARCHAR, permitedusers VARCHAR, locked BOOLEAN, hidden BOOLEAN, sessionskip BOOLEAN)"
            },
            { "vorstellungscooldown", "CREATE TABLE IF NOT EXISTS vorstellungscooldown (user_id BIGINT, time BIGINT)" },
            { "warnreasons", "CREATE TABLE IF NOT EXISTS warnreasons (reason TEXT, custom_id VARCHAR)" },
            {
                "warns",
                "CREATE TABLE IF NOT EXISTS warns (userid BIGINT, punisherid BIGINT, datum BIGINT, description VARCHAR, perma BOOLEAN, caseid VARCHAR)"
            }
        };
        var progressBar = new ConsoleProgressBar(tableCommands.Count);

        foreach (var kvp in tableCommands)
        {
            var tableName = kvp.Key;
            var createTableCommand = kvp.Value;

            await using var cmdCreate = new NpgsqlCommand(createTableCommand, conn);
            await cmdCreate.ExecuteNonQueryAsync();
            //CurrentApplication.Logger.Debug($"Table {tableName} initialized or updated.");
            progressBar.Increment();
            await Task.Delay(25);
        }

        await conn.CloseAsync();

        CurrentApplication.Logger.Information("Database tables initialized.");
        await UpdateTables();
    }

    private static async Task UpdateTables()
    {
        var dbstring = GetConnectionString();
        await using var conn = new NpgsqlConnection(dbstring);
        CurrentApplication.Logger.Information("Updating database tables...");

        await conn.OpenAsync();

        var columnUpdates = new Dictionary<string, Dictionary<string, string>>
        {
            {
                "reasonmap",
                new Dictionary<string, string>
                {
                    { "key", "ALTER TABLE reasonmap ADD COLUMN IF NOT EXISTS key TEXT" },
                    { "text", "ALTER TABLE reasonmap ADD COLUMN IF NOT EXISTS text TEXT" }
                }
            },
            {
                "banreasons",
                new Dictionary<string, string>
                {
                    { "reason", "ALTER TABLE banreasons ADD COLUMN IF NOT EXISTS reason TEXT" },
                    { "custom_id", "ALTER TABLE banreasons ADD COLUMN IF NOT EXISTS custom_id VARCHAR" }
                }
            },
            {
                "flags",
                new Dictionary<string, string>
                {
                    { "userid", "ALTER TABLE flags ADD COLUMN IF NOT EXISTS userid BIGINT" },
                    { "punisherid", "ALTER TABLE flags ADD COLUMN IF NOT EXISTS punisherid BIGINT" },
                    { "datum", "ALTER TABLE flags ADD COLUMN IF NOT EXISTS datum BIGINT" },
                    { "description", "ALTER TABLE flags ADD COLUMN IF NOT EXISTS description VARCHAR" },
                    { "caseid", "ALTER TABLE flags ADD COLUMN IF NOT EXISTS caseid VARCHAR" }
                }
            },
            {
                "tempvoice",
                new Dictionary<string, string>
                {
                    { "channelid", "ALTER TABLE tempvoice ADD COLUMN IF NOT EXISTS channelid BIGINT" },
                    { "ownerid", "ALTER TABLE tempvoice ADD COLUMN IF NOT EXISTS ownerid BIGINT" },
                    { "lastedited", "ALTER TABLE tempvoice ADD COLUMN IF NOT EXISTS lastedited BIGINT" },
                    { "laststatusedited", "ALTER TABLE tempvoice ADD COLUMN IF NOT EXISTS laststatusedited BIGINT" },
                    { "channelmods", "ALTER TABLE tempvoice ADD COLUMN IF NOT EXISTS channelmods VARCHAR" }
                }
            },
            {
                "tempvoicesession",
                new Dictionary<string, string>
                {
                    { "userid", "ALTER TABLE tempvoicesession ADD COLUMN IF NOT EXISTS userid BIGINT" },
                    { "channelname", "ALTER TABLE tempvoicesession ADD COLUMN IF NOT EXISTS channelname VARCHAR" },
                    {
                        "channelbitrate", "ALTER TABLE tempvoicesession ADD COLUMN IF NOT EXISTS channelbitrate INTEGER"
                    },
                    { "channellimit", "ALTER TABLE tempvoicesession ADD COLUMN IF NOT EXISTS channellimit INTEGER" },
                    { "blockedusers", "ALTER TABLE tempvoicesession ADD COLUMN IF NOT EXISTS blockedusers VARCHAR" },
                    { "permitedusers", "ALTER TABLE tempvoicesession ADD COLUMN IF NOT EXISTS permitedusers VARCHAR" },
                    { "locked", "ALTER TABLE tempvoicesession ADD COLUMN IF NOT EXISTS locked BOOLEAN" },
                    { "hidden", "ALTER TABLE tempvoicesession ADD COLUMN IF NOT EXISTS hidden BOOLEAN" },
                    { "sessionskip", "ALTER TABLE tempvoicesession ADD COLUMN IF NOT EXISTS sessionskip BOOLEAN" }
                }
            },
            {
                "vorstellungscooldown",
                new Dictionary<string, string>
                {
                    { "user_id", "ALTER TABLE vorstellungscooldown ADD COLUMN IF NOT EXISTS user_id BIGINT" },
                    { "time", "ALTER TABLE vorstellungscooldown ADD COLUMN IF NOT EXISTS time BIGINT" }
                }
            },
            {
                "warnreasons",
                new Dictionary<string, string>
                {
                    { "reason", "ALTER TABLE warnreasons ADD COLUMN IF NOT EXISTS reason TEXT" },
                    { "custom_id", "ALTER TABLE warnreasons ADD COLUMN IF NOT EXISTS custom_id VARCHAR" }
                }
            },
            {
                "warns",
                new Dictionary<string, string>
                {
                    { "userid", "ALTER TABLE warns ADD COLUMN IF NOT EXISTS userid BIGINT" },
                    { "punisherid", "ALTER TABLE warns ADD COLUMN IF NOT EXISTS punisherid BIGINT" },
                    { "datum", "ALTER TABLE warns ADD COLUMN IF NOT EXISTS datum BIGINT" },
                    { "description", "ALTER TABLE warns ADD COLUMN IF NOT EXISTS description VARCHAR" },
                    { "perma", "ALTER TABLE warns ADD COLUMN IF NOT EXISTS perma BOOLEAN" },
                    { "caseid", "ALTER TABLE warns ADD COLUMN IF NOT EXISTS caseid VARCHAR" }
                }
            }
        };


        var commandCount = 0;
        foreach (var tableKvp in columnUpdates)
        {
            foreach (var columnKvp in tableKvp.Value)
            {
                commandCount++;
            }
        }

        var progressBar = new ConsoleProgressBar(commandCount);


        foreach (var tableKvp in columnUpdates)
        {
            var tableName = tableKvp.Key;
            foreach (var columnKvp in tableKvp.Value)
            {
                var columnName = columnKvp.Key;
                var alterColumnCommand = columnKvp.Value;

                await using var cmdAlter = new NpgsqlCommand(alterColumnCommand, conn);
                await cmdAlter.ExecuteNonQueryAsync();
                progressBar.Increment();
                await Task.Delay(25);
            }
        }

        await conn.CloseAsync();
        CurrentApplication.Logger.Information("Database tables updated.");
    }
}