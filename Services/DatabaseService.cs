#region

using System.Data;

#endregion

namespace AGC_Management.Services;

public static class DatabaseService
{

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
            using var dbConnection = new NpgsqlConnection(GetConnectionString());
            using var cmd = new NpgsqlCommand(sql, dbConnection);
            return cmd.ExecuteReader();
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

        await using NpgsqlConnection connection = new(GetConnectionString());
        await connection.OpenAsync();

        await using NpgsqlCommand command = new(insertQuery, connection);
        foreach (var kvp in columnValuePairs)
        {
            NpgsqlParameter parameter = new($"@{kvp.Key}", kvp.Value);
            command.Parameters.Add(parameter);
        }

        await command.ExecuteNonQueryAsync();
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

        var connection = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>(); 

        await using NpgsqlCommand command = connection.CreateCommand(selectQuery);
        if (whereConditions != null && whereConditions.Count > 0)
            foreach (var condition in whereConditions)
            {
                NpgsqlParameter parameter = new($"@{condition.Key}", condition.Value);
                command.Parameters.Add(parameter);
            }

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
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
        
        var connection = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();

        await using NpgsqlCommand command = connection.CreateCommand(deleteQuery);
        if (whereConditions != null && whereConditions.Count > 0)
            foreach (var condition in whereConditions)
            {
                NpgsqlParameter parameter = new($"@{condition.Key}", condition.Value.value);
                command.Parameters.Add(parameter);
            }

        rowsAffected = await command.ExecuteNonQueryAsync();

        return rowsAffected;
    }


    public static async Task InitializeAndUpdateDatabaseTables()
    {
        var dbstring = GetConnectionString();
        var conn = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        CurrentApplication.Logger.Information("Initializing database tables...");
        
        var tableCommands = new Dictionary<string, string>
        {
            { "reasonmap", "CREATE TABLE IF NOT EXISTS reasonmap (key TEXT, text TEXT)" },
            {
                "levelingdata",
                "CREATE TABLE IF NOT EXISTS levelingdata (userid BIGINT, current_xp INTEGER, current_level INTEGER, last_text_reward BIGINT DEFAULT 0, last_vc_reward BIGINT DEFAULT 0, pingactive BOOLEAN DEFAULT true, UNIQUE (userid))"
            },
            {
                "levelingsettings",
                "CREATE TABLE IF NOT EXISTS levelingsettings (guildid BIGINT, text_active BOOLEAN DEFAULT false, vc_active BOOLEAN DEFAULT FALSE, text_multi FLOAT DEFAULT 1.0, vc_multi FLOAT DEFAULT 1.0, levelupchannelid BIGINT, levelupmessage TEXT, levelupmessagereward TEXT, retainroles BOOLEAN DEFAULT true, lastrecalc BIGINT DEFAULT 0)"
            },
            { "level_rewards", "CREATE TABLE IF NOT EXISTS level_rewards (level INTEGER, roleid BIGINT)" },
            {
                "level_multiplicatoroverrideroles",
                "CREATE TABLE IF NOT EXISTS level_multiplicatoroverrideroles (roleid BIGINT, multiplicator FLOAT)"
            },
            { "level_excludedchannels", "CREATE TABLE IF NOT EXISTS level_excludedchannels (channelid BIGINT)" },
            { "level_excludedroles", "CREATE TABLE IF NOT EXISTS level_excludedroles (roleid BIGINT)" },
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

            await using var cmdCreate = conn.CreateCommand(createTableCommand);
            await cmdCreate.ExecuteNonQueryAsync();
            //CurrentApplication.Logger.Debug($"Table {tableName} initialized or updated.");
            progressBar.Increment();
            await Task.Delay(25);
        }



        CurrentApplication.Logger.Information("Database tables initialized.");
        await UpdateTables();
    }

    private static async Task UpdateTables()
    {
        var dbstring = GetConnectionString();
        var conn = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
        CurrentApplication.Logger.Information("Updating database tables...");


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
                "levelingdata",
                new Dictionary<string, string>
                {
                    { "userid", "ALTER TABLE levelingdata ADD COLUMN IF NOT EXISTS userid BIGINT" },
                    { "current_xp", "ALTER TABLE levelingdata ADD COLUMN IF NOT EXISTS current_xp INTEGER" },
                    { "current_level", "ALTER TABLE levelingdata ADD COLUMN IF NOT EXISTS current_level INTEGER" },
                    {
                        "last_text_reward",
                        "ALTER TABLE levelingdata ADD COLUMN IF NOT EXISTS last_text_reward BIGINT DEFAULT 0"
                    },
                    {
                        "last_vc_reward",
                        "ALTER TABLE levelingdata ADD COLUMN IF NOT EXISTS last_vc_reward BIGINT DEFAULT 0"
                    },
                    {
                        "pingactive",
                        "ALTER TABLE levelingdata ADD COLUMN IF NOT EXISTS pingactive BOOLEAN DEFAULT true"
                    },
                    {
                        "unique_index",
                        "CREATE UNIQUE INDEX IF NOT EXISTS idx_levelingdata_userid ON levelingdata (userid)"
                    }
                }
            },
            {
                "levelingsettings",
                new Dictionary<string, string>
                {
                    { "guildid", "ALTER TABLE levelingsettings ADD COLUMN IF NOT EXISTS guildid BIGINT" },
                    {
                        "text_active",
                        "ALTER TABLE levelingsettings ADD COLUMN IF NOT EXISTS text_active BOOLEAN DEFAULT false"
                    },
                    {
                        "vc_active",
                        "ALTER TABLE levelingsettings ADD COLUMN IF NOT EXISTS vc_active BOOLEAN DEFAULT false"
                    },
                    {
                        "text_multi",
                        "ALTER TABLE levelingsettings ADD COLUMN IF NOT EXISTS text_multi FLOAT DEFAULT 1.0"
                    },
                    { "vc_multi", "ALTER TABLE levelingsettings ADD COLUMN IF NOT EXISTS vc_multi FLOAT DEFAULT 1.0" },
                    {
                        "levelupchannelid",
                        "ALTER TABLE levelingsettings ADD COLUMN IF NOT EXISTS levelupchannelid BIGINT"
                    },
                    { "levelupmessage", "ALTER TABLE levelingsettings ADD COLUMN IF NOT EXISTS levelupmessage TEXT" },
                    {
                        "levelupmessagereward",
                        "ALTER TABLE levelingsettings ADD COLUMN IF NOT EXISTS levelupmessagereward TEXT"
                    },
                    {
                        "retainroles",
                        "ALTER TABLE levelingsettings ADD COLUMN IF NOT EXISTS retainroles BOOLEAN DEFAULT true"
                    },
                    {
                        "lastrecalc",
                        "ALTER TABLE levelingsettings ADD COLUMN IF NOT EXISTS lastrecalc BIGINT DEFAULT 0"
                    }
                }
            },

            {
                "level_excludedchannels", new Dictionary<string, string>
                {
                    { "channelid", "ALTER TABLE level_excludedchannels ADD COLUMN IF NOT EXISTS channelid BIGINT" }
                }
            },

            {
                "level_excludedroles", new Dictionary<string, string>
                {
                    { "roleid", "ALTER TABLE level_excludedroles ADD COLUMN IF NOT EXISTS roleid BIGINT" }
                }
            },

            {
                "level_multiplicatoroverrideroles", new Dictionary<string, string>
                {
                    { "roleid", "ALTER TABLE level_multiplicatoroverrideroles ADD COLUMN IF NOT EXISTS roleid BIGINT" },
                    {
                        "multiplicator",
                        "ALTER TABLE level_multiplicatoroverrideroles ADD COLUMN IF NOT EXISTS multiplicator FLOAT"
                    }
                }
            },

            {
                "level_rewards", new Dictionary<string, string>
                {
                    { "level", "ALTER TABLE level_rewards ADD COLUMN IF NOT EXISTS level INTEGER" },
                    { "roleid", "ALTER TABLE level_rewards ADD COLUMN IF NOT EXISTS roleid BIGINT" }
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

                await using var cmdAlter = conn.CreateCommand(alterColumnCommand);
                await cmdAlter.ExecuteNonQueryAsync();
                progressBar.Increment();
                await Task.Delay(25);
            }
        }
        
        await InitLeveling();
        CurrentApplication.Logger.Information("Database tables updated.");
    }


    private static async Task InitLeveling()
    {
        ulong targetGuildId = ulong.Parse(BotConfig.GetConfig()["ServerConfig"]["ServerId"]);
        var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
       
        await using var cmd = con.CreateCommand($"SELECT * FROM levelingsettings WHERE guildid = '{targetGuildId}'");
        await using var reader = await cmd.ExecuteReaderAsync();
        var result = await reader.ReadAsync();
        await reader.CloseAsync();
        if (!result)
        {
            await using var cmd2 = con.CreateCommand(
                "INSERT INTO levelingsettings (guildid, text_active, vc_active, text_multi, vc_multi, levelupchannelid, levelupmessage, levelupmessagereward, retainroles, lastrecalc) VALUES (@guildid, false, false, 1.0, 1.0, 0, 'Herzlichen Glückwunsch {usermention}! Du bist nun Level {level}!', 'Herzlichen Glückwunsch {usermention}! Du bist nun Level {level}!', true, 0)");
            cmd2.Parameters.AddWithValue("guildid", (long)targetGuildId);
            await cmd2.ExecuteNonQueryAsync();
        }
    }
}