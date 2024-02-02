using Newtonsoft.Json;
using AGC_Management.Enums;

namespace AGC_Management.Services
{
    public sealed class CachingService
    {
        private static readonly string cacheDir = Path.Combine("botcache"); 

        public static async Task<string> GetCacheValue(FileCacheType cachefile, string key)
        {
            string fullPath = Path.Combine(cacheDir, cachefile.ToString()); 
            await createCacheFile(fullPath);
            var cache = await File.ReadAllTextAsync(fullPath);
            var cacheObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(cache);
            return cacheObject.TryGetValue(key, out var value) ? value : null;
        }

        public static async Task SetCacheValue(FileCacheType cachefile, string key, string value)
        {
            string fullPath = Path.Combine(cacheDir, cachefile.ToString()); 
            await createCacheFile(fullPath);
            var cache = await File.ReadAllTextAsync(fullPath);
            var cacheObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(cache) ?? new Dictionary<string, string>();
            cacheObject[key] = value;
            await File.WriteAllTextAsync(fullPath, JsonConvert.SerializeObject(cacheObject));
        }

        private static async Task createCacheFile(string fullPath)
        {
            if (!Directory.Exists(cacheDir)) 
            {
                Directory.CreateDirectory(cacheDir);
            }
            if (!File.Exists(fullPath))
            {
                await File.WriteAllTextAsync(fullPath, "{}");
            }
        }
        
        public static void ClearCompleteCache()
        {
            if (Directory.Exists(cacheDir))
            {
                Directory.Delete(cacheDir, true);
            }
        }
        
        public static void ClearCacheFile(FileCacheType cachefile)
        {
            string fullPath = Path.Combine(cacheDir, cachefile.ToString());
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}