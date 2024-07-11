using System.Globalization;
using UnityEngine;

namespace ClientUI.UI;

public static class RectSaveExtensions
{
    // Window Anchors helpers
    internal static string RectAnchorsToString(this RectTransform rect)
    {
        if (!rect)
            throw new ArgumentNullException("rect");

        return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", new object[]
        {
            rect.anchorMin.x,
            rect.anchorMin.y,
            rect.anchorMax.x,
            rect.anchorMax.y
        });
    }

    internal static void SetAnchorsFromString(this RectTransform panel, string stringAnchors)
    {
        if (string.IsNullOrEmpty(stringAnchors))
            throw new ArgumentNullException("stringAnchors");

        if (stringAnchors.Contains(" "))
            // outdated save data, not worth recovering just reset it.
            throw new Exception("invalid save data, resetting.");

        string[] split = stringAnchors.Split(',');

        if (split.Length != 4)
            throw new Exception($"stringAnchors split is unexpected length: {split.Length}");

        Vector4 anchors;
        anchors.x = float.Parse(split[0], CultureInfo.InvariantCulture);
        anchors.y = float.Parse(split[1], CultureInfo.InvariantCulture);
        anchors.z = float.Parse(split[2], CultureInfo.InvariantCulture);
        anchors.w = float.Parse(split[3], CultureInfo.InvariantCulture);

        panel.anchorMin = new Vector2(anchors.x, anchors.y);
        panel.anchorMax = new Vector2(anchors.z, anchors.w);
    }

    internal static string RectPositionToString(this RectTransform rect)
    {
        if (!rect)
            throw new ArgumentNullException("rect");

        return string.Format(CultureInfo.InvariantCulture, "{0},{1}", new object[]
        {
            rect.anchoredPosition.x, rect.anchoredPosition.y
        });
    }

    internal static void SetPositionFromString(this RectTransform rect, string stringPosition)
    {
        if (string.IsNullOrEmpty(stringPosition))
            throw new ArgumentNullException(stringPosition);

        if (stringPosition.Contains(" "))
            // outdated save data, not worth recovering just reset it.
            throw new Exception("invalid save data, resetting.");

        string[] split = stringPosition.Split(',');

        if (split.Length != 2)
            throw new Exception($"stringPosition split is unexpected length: {split.Length}");

        Vector3 vector = rect.anchoredPosition;
        vector.x = float.Parse(split[0], CultureInfo.InvariantCulture);
        vector.y = float.Parse(split[1], CultureInfo.InvariantCulture);
        rect.anchoredPosition = vector;
    }
}