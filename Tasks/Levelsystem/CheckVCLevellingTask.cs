namespace AGC_Management.Tasks.Levelsystem;

public static class CheckVCLevellingTask
{
    public static async Task LaunchLoops()
    {
        await StartCheckVCLevelling();
    }

    private static async Task StartCheckVCLevelling()
    {
        await Task.Delay(TimeSpan.FromSeconds(5));
        while (true)
        {
            // implemtation here

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
}