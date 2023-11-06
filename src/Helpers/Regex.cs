namespace AGC_Management.Helpers
{
    public static class RegexPatterns
    {
        public const string INVITE =
            @"(?:https?://)?(?:(?:\w+\.))?discord(?:(?:app)?\.com/invite|\.gg)/(?<code>[a-z0-9-]+)(?:\?\S*)?(?:#\S*)?";
    }
}