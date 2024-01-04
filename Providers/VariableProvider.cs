namespace AGC_Management.Providers;

public static class VariableProvider
{
    public static string GetCurrentUnixTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
    }
}