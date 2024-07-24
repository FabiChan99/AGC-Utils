namespace AGC_Management.Providers;

public enum ImageStoreType
{
    Flag,
    Warn
}

public static class ImageStoreProvider
{
    public static string GetFlagImageStorePath()
    {
        return BotConfig.GetConfig()["FlagImageStore"]["FlagImagePath"];
    }
    
    public static string GetWarnImageStorePath()
    {
        return BotConfig.GetConfig()["FlagImageStore"]["WarnImagePath"];
    }

    public static string GetImageStoreDomain()
    {
        return BotConfig.GetConfig()["FlagImageStore"]["Domain"];
    }
    
    private static string GetImageStorePath(ImageStoreType type)
    {
        return type switch
        {
            ImageStoreType.Flag => GetFlagImageStorePath(),
            ImageStoreType.Warn => GetWarnImageStorePath(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }


    public static string SaveImage(string fileName, byte[] image, ImageStoreType storeType)
    {
        var path = GetImageStorePath(storeType);
        var domain = GetImageStoreDomain();
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        var fullPath = Path.Combine(path, fileName);
        var prefix = "https://";
        File.WriteAllBytes(fullPath, image);
        return $"{prefix}{domain}/{fileName}";
    }
}