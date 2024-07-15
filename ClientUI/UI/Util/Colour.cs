using UnityEngine;

namespace ClientUI.UI.Util;

public static class Colour
{
    // Colour constants
    public static readonly Color DefaultBarColour = new Color(0.5f, 0.8f, 0.1f);
    public static readonly Color HighlightColour = Color.yellow;
    public static readonly Color LowLightColour = Color.red;
    
    /// <summary>
    /// Parses a colour string to extract the colour or colour map.
    /// For colour maps, the appropriate colour is calculated based on the percentage provided.
    /// </summary>
    /// <param name="colourString"></param>
    /// <param name="percentage"></param>
    /// <returns></returns>
    public static Color ParseColour(string colourString, float percentage = 0)
    {
        if (string.IsNullOrEmpty(colourString)) return DefaultBarColour;
        if (colourString.StartsWith("@"))
        {
            var colourStrings = colourString.Split("@", StringSplitOptions.RemoveEmptyEntries);
            if (colourStrings.Length == 0) return DefaultBarColour;
            if (colourStrings.Length == 1)
            {
                if (!ColorUtility.TryParseHtmlString(colourStrings[0], out var onlyColour)) onlyColour = DefaultBarColour;
                return onlyColour;
            }
            
            var internalRange = percentage * (colourStrings.Length - 1);
            var index = (int)Math.Floor(internalRange);
            internalRange -= index;
            if (!ColorUtility.TryParseHtmlString(colourStrings[index], out var colour1)) colour1 = DefaultBarColour;
            if (!ColorUtility.TryParseHtmlString(colourStrings[index + 1], out var colour2)) colour2 = DefaultBarColour;
            return Color.Lerp(colour1, colour2, internalRange);
        }

        return !ColorUtility.TryParseHtmlString(colourString, out var parsedColour) ? DefaultBarColour : parsedColour;
    }
}