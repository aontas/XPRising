using System.Globalization;
using UnityEngine;

namespace ClientUI.UI;

public static class RectExtensions
{
    // Window Anchors helpers
    internal static string RectAnchorsToString(this RectTransform rect)
    {
        if (!rect)
            throw new ArgumentNullException("rect");

        return string.Format(CultureInfo.InvariantCulture, "{0},{1}", new object[]
        {
            rect.rect.width,
            rect.rect.height
        });
    }

    internal static void SetAnchorsFromString(this RectTransform panel, string stringAnchors)
    {
        if (string.IsNullOrEmpty(stringAnchors))
            throw new ArgumentNullException("stringAnchors");

        string[] split = stringAnchors.Split(',');

        if (split.Length != 2)
            throw new Exception($"stringAnchors split is unexpected length: {split.Length}");

        var width = float.Parse(split[0], CultureInfo.InvariantCulture);
        var height = float.Parse(split[1], CultureInfo.InvariantCulture);

        panel.sizeDelta = new Vector2(width, height);
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

        string[] split = stringPosition.Split(',');

        if (split.Length != 2)
            throw new Exception($"stringPosition split is unexpected length: {split.Length}");

        Vector2 vector = rect.anchoredPosition;
        vector.x = float.Parse(split[0], CultureInfo.InvariantCulture);
        vector.y = float.Parse(split[1], CultureInfo.InvariantCulture);
        rect.anchoredPosition = vector;
    }

    internal static void SetPivot(this RectTransform rect, Vector2 pivot)
    {
        Vector2 size = rect.rect.size;
        Vector2 deltaPivot = rect.pivot - pivot;
        Vector3 deltaPosition = new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y);
        rect.pivot = pivot;
        rect.localPosition -= deltaPosition;
    }
}