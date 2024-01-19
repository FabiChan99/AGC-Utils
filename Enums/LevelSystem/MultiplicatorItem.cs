using DisCatSharp.ApplicationCommands.Attributes;

namespace AGC_Management.Enums.LevelSystem;

public enum OverrideMultiplicatorItem
{
    [ChoiceName("0.01x")]
    Hundredth,
    [ChoiceName("0.25x")]
    Quarter,
    [ChoiceName("0.5x")]
    Half,
    [ChoiceName("1x")]
    One,
    [ChoiceName("1.5x")]
    OneAndHalf,
    [ChoiceName("2x")]
    Two,
    [ChoiceName("3x")]
    Three,
    [ChoiceName("4x")]
    Four,
    [ChoiceName("5x")]
    Five,
}

public enum MultiplicatorItem
{
    [ChoiceName("Deaktiviert")]
    Disabled,
    [ChoiceName("0.25x")]
    Quarter,
    [ChoiceName("0.5x")]
    Half,
    [ChoiceName("1x")]
    One,
    [ChoiceName("1.5x")]
    OneAndHalf,
    [ChoiceName("2x")]
    Two,
    [ChoiceName("3x")]
    Three,
    [ChoiceName("4x")]
    Four,
    [ChoiceName("5x")]
    Five,
}