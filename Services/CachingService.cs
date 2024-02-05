#region

using System.Text;
using AGC_Management.Enums;
using NpgsqlTypes;

#endregion

namespace AGC_Management.Services
{
    public sealed class CachingService
    {
        public static async Task<string> GetCacheValue(CustomDatabaseCacheType cachefile, string key)
        {
            var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
            await using var com =
                con.CreateCommand(
                    "SELECT content->>@key FROM cachetable WHERE cachetype = @cachetype AND content ? @key");
            com.Parameters.AddWithValue("cachetype", cachefile.ToString());
            com.Parameters.AddWithValue("key", key);
            var result = await com.ExecuteScalarAsync();
            return result?.ToString() ?? "";
        }

        public static async Task SetCacheValue(CustomDatabaseCacheType cachefile, string key, string value)
        {
            var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
            var query = @"
                        INSERT INTO cachetable (cachetype, content) 
                        VALUES (@cachetype, jsonb_build_object(@key, @value::jsonb)) 
                        ON CONFLICT (cachetype) 
                        DO UPDATE 
                        SET content = jsonb_set(
                            coalesce(cachetable.content, '{}'::jsonb),
                            array[@key],
                            @value::jsonb,
                            true
                        );
                    ";
            await using var com = con.CreateCommand(query);
            com.Parameters.AddWithValue("cachetype", cachefile.ToString());
            com.Parameters.AddWithValue("key", key);
            // Achten Sie darauf, den Wert korrekt als JSONB zu formatieren.
            com.Parameters.AddWithValue("value", NpgsqlDbType.Jsonb,
                $"\"{value}\""); // Der Wert muss als gültiger JSONB-String übergeben werden.

            await com.ExecuteNonQueryAsync();
        }

        public static async Task SetCacheValueAsBase64(CustomDatabaseCacheType cachefile, string key, string value)
        {
            var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
            await SetCacheValue(cachefile, key, base64);
        }

        public static async Task<string> GetCacheValueAsBase64(CustomDatabaseCacheType cachefile, string key)
        {
            var base64 = await GetCacheValue(cachefile, key);
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }


        public static void ClearCompleteCache()
        {
            var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
            var command = con.CreateCommand("DELETE FROM cachetable");
            command.ExecuteNonQuery();
        }


        public static async Task DeleteCacheObject(CustomDatabaseCacheType cachefile, string key)
        {
            var con = CurrentApplication.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
            var query = @"
                        UPDATE cachetable
                        SET content = jsonb_strip_nulls(
                            jsonb_set(content, array[@key], 'null')
                        )
                        WHERE cachetype = @cachetype;
                    ";
            await using var cmd = con.CreateCommand(query);
            cmd.Parameters.AddWithValue("cachetype", cachefile.ToString());
            cmd.Parameters.AddWithValue("key", key);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}