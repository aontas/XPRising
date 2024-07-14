using XPRising.Systems;
using XPRising.Transport;

namespace XPRising.Models;

public struct PlayerPreferences
{
    public bool LoggingWanted = false;
    public bool LoggingExp = false;
    public bool LoggingMastery = false;
    public bool IgnoringInvites = false;
    public string Language = L10N.DefaultLanguage;
    public int TextSize = Plugin.DefaultTextSize;
    public Actions.BarState UIProgressDisplay = Actions.BarState.Active;

    public PlayerPreferences()
    {
    }

    public static int ConvertTextToSize(string textSize)
    {
        return textSize switch
        {
            "tiny" => 10,
            "small" => 12,
            "normal" => 16,
            _ => 12
        };
    }
    
    public static string ConvertSizeToText(int size)
    {
        return size switch
        {
            10 => "tiny",
            12 => "small",
            16 => "normal",
            _ => "small"
        };
    }
}