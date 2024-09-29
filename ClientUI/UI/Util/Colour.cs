using UnityEngine;

namespace ClientUI.UI.Util;

public static class Colour
{
    // Base colour palette
    public static readonly Color Level1 = new(0.64f, 0, 0);
    public static readonly Color Level2 = new(0.72f, 0.43f, 0);
    public static readonly Color Level3 = new(1, 0.83f, 0.45f);
    public static readonly Color Level4 = new(0.47f, 0.74f, 0.38f);
    public static readonly Color Level5 = new(0.18f, 0.53f, 0.67f);
    
    // Colour constants
    public static readonly Color DefaultBar = Level4;
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
    
    private static float Contrast(float l1, float l2) {
        return l1 < l2
            ? ((l2 + 0.05f) / (l1 + 0.05f))
            : ((l1 + 0.05f) / (l2 + 0.05f));
    }

    private static float CalculateRelativeLuminance(float r, float g, float b)
    {
        float rv = (r <= 0.04045f) ? r / 12.92f : MathF.Pow((r + 0.055f) / 1.055f, 2.4f);
        float gv = (g <= 0.04045f) ? g / 12.92f : MathF.Pow((g + 0.055f) / 1.055f, 2.4f);
        float bv = (b <= 0.04045f) ? b / 12.92f : MathF.Pow((b + 0.055f) / 1.055f, 2.4f);

        return 0.2126f * rv + 0.7152f * gv + 0.0722f * bv;
    }
	
    public static Color TextColourForBackground(Color background)
    {
        float backgroundLuminance = CalculateRelativeLuminance(background.r, background.g, background.b);
        float whiteContrastRatio = Contrast(backgroundLuminance, 1f);
        float blackContrastRatio = Contrast(backgroundLuminance, 0f);
        return whiteContrastRatio > blackContrastRatio ? Color.white : Color.black;
    }
}