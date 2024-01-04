#region

using AGC_Management.Services;

#endregion

namespace AGC_Management.Utils
{
    public static class ReasonTemplateResolver
    {
        public static async Task<string> Resolve(string template)
        {
            var replacements = await GetReplacements();
            foreach (var (key, value) in replacements)
            {
                template = template.Replace("-" + key, value);
            }

            return template;
        }

        public static async Task<Dictionary<string, string>> GetReplacements()
        {
            var replacements = new Dictionary<string, string>();
            string connectionString = DatabaseService.GetConnectionString();

            await using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            await using var cmd = new NpgsqlCommand("SELECT key, text FROM reasonmap", conn);
            await using var reader = cmd.ExecuteReader();
            while (await reader.ReadAsync())
            {
                replacements[reader.GetString(0)] = reader.GetString(1);
            }

            return replacements;
        }

        public static async Task<bool> AddReplacement(string key, string text)
        {
            string connectionString = DatabaseService.GetConnectionString();

            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                await using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM reasonmap WHERE key = @key", conn))
                {
                    checkCmd.Parameters.AddWithValue("key", key);
                    int exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (exists > 0)
                    {
                        return false;
                    }
                }

                await using (var cmd = new NpgsqlCommand("INSERT INTO reasonmap (key, text) VALUES (@key, @text)",
                                 conn))
                {
                    cmd.Parameters.AddWithValue("key", key);
                    cmd.Parameters.AddWithValue("text", text);
                    await cmd.ExecuteNonQueryAsync();
                }

                return true;
            }
            catch (NpgsqlException ex)
            {
                return false;
            }
        }

        public static async Task<bool> RemoveReplacement(string key)
        {
            string connectionString = DatabaseService.GetConnectionString();

            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand("DELETE FROM reasonmap WHERE key = @key", conn);
                cmd.Parameters.AddWithValue("key", key);

                int affectedRows = await cmd.ExecuteNonQueryAsync();
                return affectedRows > 0;
            }
            catch (NpgsqlException ex)
            {
                return false;
            }
        }
    }
}