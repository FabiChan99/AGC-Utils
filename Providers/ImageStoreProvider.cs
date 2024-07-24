namespace AGC_Management.Providers;

public static class ImageStoreProvider
{
    public static string GetImageStorePath()
    {
        return BotConfig.GetConfig()["FlagImageStore"]["Path"];
    }

    public static string GetImageStoreDomain()
    {
        return BotConfig.GetConfig()["FlagImageStore"]["Domain"];
    }


    public static string SaveImage(string fileName, byte[] image)
    {
        var path = GetImageStorePath();
        var domain = GetImageStoreDomain();
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        var fullPath = Path.Combine(path, fileName);
        var prefix = "https://";
        File.WriteAllBytes(fullPath, image);
        return $"{prefix}{domain}/{fileName}";
    }
}