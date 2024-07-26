using ClientUI.UI.Util;
using UnityEngine;
using UnityEngine.UI;
using XPShared.Transport.Messages;
using UIFactory = ClientUI.UniverseLib.UI.UIFactory;

namespace ClientUI.UI.Panel;

public class ProgressBarPanel
{
    private readonly GameObject _contentRoot;
    public ProgressBarPanel(GameObject root)
    {
        _contentRoot = root;
        
        UIFactory.SetLayoutGroup<VerticalLayoutGroup>(_contentRoot, true, false, true, true, 0);
        
        UpdateGroupActiveState();
    }
    
    public bool Active
    {
        get => _contentRoot.active;
        set => _contentRoot.SetActive(value);
    }
    
    private const int Spacing = 4;
    private const int VPadding = 2;
    private const int HPadding = 2;
    private readonly Vector4 _paddingVector = new Vector4(VPadding, VPadding, HPadding, HPadding);

    private readonly Dictionary<string, ProgressBar> _bars = new();
    private readonly Dictionary<string, Group> _groups = new();

    private struct Group
    {
        public GameObject GameObject;
        public RectTransform RectTransform;

        public Group(RectTransform rectTransform, GameObject gameObject)
        {
            RectTransform = rectTransform;
            GameObject = gameObject;
        }
    }

    private void UpdateGroupActiveState()
    {
        foreach (var (_, group) in _groups)
        {
            var activeBarCount = group.RectTransform.GetAllChildren().Count(transform => transform.gameObject.active);
            group.GameObject.SetActive(activeBarCount > 0);
        }
    }
    
    public void ChangeProgress(ProgressSerialisedMessage data)
    {
        if (!_bars.TryGetValue(data.Label, out var progressBar))
        {
            progressBar = AddBar(data.Group, data.Label);
        }
        
        var validatedProgress = Math.Clamp(data.ProgressPercentage, 0f, 1f);
        var colour = Colour.ParseColour(data.Colour, validatedProgress);
        progressBar.SetProgress(validatedProgress, $"{data.Level:D2}", $"{data.Tooltip} ({validatedProgress:P})", data.Active, colour, data.Change);

        if (data.Change != "")
        {
            FloatingText.SpawnFloatingText(_contentRoot, data.Change, Colour.Highlight);
        }
    }

    internal void Reset()
    {
        foreach (var (_, group) in _groups)
        {
            GameObject.Destroy(group.GameObject);
        }
        _groups.Clear();
        _bars.Clear();
        UpdateGroupActiveState();
    }

    private ProgressBar AddBar(string groupName, string label)
    {
        if (!_groups.TryGetValue(groupName, out var group))
        {
            var groupGameObject = UIFactory.CreateVerticalGroup(_contentRoot, groupName, true, false, true, true, Spacing, padding: _paddingVector);
            group.GameObject = groupGameObject;
            group.RectTransform = groupGameObject.GetComponent<RectTransform>();
            _groups.Add(groupName, group);
        }
        var progressBar = new ProgressBar(group.GameObject, Colour.DefaultBar);
        _bars.Add(label, progressBar);
        progressBar.ProgressBarMinimised += (_, _) => { UpdateGroupActiveState(); }; 
        
        return progressBar;
    }
}