using UnityEngine;

namespace ClientUI.UI.Util;

public static class Colour
{
    // Colour constants
    public static readonly Color DefaultBar = new(0.5f, 0.8f, 0.1f);
    public static readonly Color Highlight = Color.yellow;
    public static readonly Color PositiveChange = Color.yellow;
    public static readonly Color NegativeChange = Color.red;

    public static readonly Color DarkBackground = new(0.07f, 0.07f, 0.07f);
    public static readonly Color PanelBackground = new(0.17f, 0.17f, 0.17f);
    public static readonly Color SliderFill = new(0.3f, 0.3f, 0.3f);
    public static readonly Color SliderHandle = new(0.5f, 0.5f, 0.5f);
    public static readonly Color CheckMark = new(0.6f, 0.7f, 0.6f);
    public static readonly Color DefaultText = Color.white;
    public static readonly Color PlaceHolderText = SliderHandle;
    
    // TODO check if the viewport objects even need a colour or image
    public static readonly Color ViewportBackground = new(0.07f, 0.07f, 0.07f);

    /// <summary>
    /// Parses a colour string to extract the colour or colour map.
    /// For colour maps, the appropriate colour is calculated based on the percentage provided.
    /// </summary>
    /// <param name="colourString"></param>
    /// <param name="percentage"></param>
    /// <returns></returns>
    public static Color ParseColour(string colourString, float percentage = 0)
    {
        if (string.IsNullOrEmpty(colourString)) return DefaultBar;
        if (colourString.StartsWith("@"))
        {
            var colourStrings = colourString.Split("@", StringSplitOptions.RemoveEmptyEntries);
            if (colourStrings.Length == 0) return DefaultBar;
            if (colourStrings.Length == 1)
            {
                if (!ColorUtility.TryParseHtmlString(colourStrings[0], out var onlyColour)) onlyColour = DefaultBar;
                return onlyColour;
            }
            
            var internalRange = percentage * (colourStrings.Length - 1);
            var index = (int)Math.Floor(internalRange);
            internalRange -= index;
            if (!ColorUtility.TryParseHtmlString(colourStrings[index], out var colour1)) colour1 = DefaultBar;
            if (!ColorUtility.TryParseHtmlString(colourStrings[index + 1], out var colour2)) colour2 = DefaultBar;
            return Color.Lerp(colour1, colour2, internalRange);
        }

        return !ColorUtility.TryParseHtmlString(colourString, out var parsedColour) ? DefaultBar : parsedColour;
    }
}