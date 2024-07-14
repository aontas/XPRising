using BepInEx.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace ClientUI.UI;

public static class LayoutGroupExtensions
{
    /// <summary>
    /// Rebuilds a layout group (and its children). Use this instead of Canvas.ForceUpdateCanvases() to immediately update a layout group.
    /// This function returns all layout groups that were updated, which can be used to refresh them again without having to gather all child layout groups another time.
    /// </summary>
    public static LayoutGroup[] FindAndRebuildLayoutGroupsImmediate(this LayoutGroup rootLayoutGroup)
    {
        LayoutGroup[] layoutGroups = rootLayoutGroup.GetComponentsInChildren<LayoutGroup>();
        Plugin.Log(LogLevel.Warning, $"START rebuilding layout group: {layoutGroups.Length} children");
        RebuildLayoutGroupsImmediate(layoutGroups);
        return layoutGroups;
    }
 
    /// <summary>
    /// Rebuilds all layout groups within an array.
    /// Use it with the array returned by FindAndRebuildLayoutGroupsImmediate() to perform multiple updates of the same set of layout groups.
    /// </summary>
    public static void RebuildLayoutGroupsImmediate(this LayoutGroup[] layoutGroups)
    {
        foreach (var layoutGroup in layoutGroups)
        {
            Plugin.Log(LogLevel.Warning, "rebuilding layout group");
            //LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.transform as RectTransform);
            if (layoutGroup.enabled)
            {
                layoutGroup.enabled = false;
                layoutGroup.enabled = true;
            }
        }
    }
}