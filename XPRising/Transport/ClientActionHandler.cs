using ProjectM.Network;
using Unity.Entities;
using XPRising.Systems;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using XPShared;
using XPShared.Transport.Messages;
using ActiveState = XPShared.Transport.Messages.ProgressSerialisedMessage.ActiveState;

namespace XPRising.Transport;

public static class ClientActionHandler
{
    private static readonly List<GlobalMasterySystem.MasteryType> DefaultMasteryList =
        Enum.GetValues<GlobalMasterySystem.MasteryType>().ToList();

    private const string BarToggleAction = "XPRising.BarMode";
    public static void HandleClientAction(User user, ClientAction action)
    {
        var sendPlayerData = false;
        var sendActionData = false;
        switch (action.Action)
        {
            case ClientAction.ActionType.Connect:
                sendPlayerData = true;
                sendActionData = true;
                break;
            case ClientAction.ActionType.ButtonClick:
                switch (action.Value)
                {
                    case BarToggleAction:
                        Actions.BarStateChanged(user);
                        sendPlayerData = true;
                        sendActionData = true;
                        break;
                }
                break;
            case ClientAction.ActionType.Disconnect:
            default:
                // Do nothing
                break;
        }
        
        if (sendPlayerData) SendPlayerData(user);
        if (sendActionData) SendActionData(user);
    }

    public static void SendPlayerData(User user)
    {
        var userUiBarPreference = Database.PlayerPreferences[user.PlatformId].UIProgressDisplay;
        
        if (Plugin.ExperienceSystemActive)
        {
            var xp = ExperienceSystem.GetXp(user.PlatformId);
            ExperienceSystem.GetLevelAndProgress(xp, out var level, out var progressPercent, out var earned, out var needed);
            SendXpData(user, level, progressPercent, earned, needed, 0);
        }

        if (Plugin.BloodlineSystemActive || Plugin.WeaponMasterySystemActive)
        {
            var masteries = new List<GlobalMasterySystem.MasteryType>();
            if (userUiBarPreference == Actions.BarState.All)
            {
                masteries = DefaultMasteryList;
            } else if (userUiBarPreference == Actions.BarState.Active)
            {
                var activeWeaponMastery = WeaponMasterySystem.WeaponToMasteryType(WeaponMasterySystem.GetWeaponType(user.LocalCharacter._Entity, out _));
                var activeBloodMastery = BloodlineSystem.BloodMasteryType(user.LocalCharacter._Entity);
                masteries.Add(activeWeaponMastery);
                masteries.Add(activeBloodMastery);
                
                if (!GlobalMasterySystem.SpellMasteryRequiresUnarmed ||
                    activeWeaponMastery == GlobalMasterySystem.MasteryType.WeaponUnarmed)
                {
                    masteries.Add(GlobalMasterySystem.MasteryType.Spell);
                }
            }
            
            var masteryData = Database.PlayerMastery[user.PlatformId];
            foreach (var (type, mastery) in masteryData)
            {
                var setActive = masteries.Contains(type);
                SendMasteryData(user, type, (float)mastery.Mastery, setActive ? ActiveState.Active : ActiveState.NotActive);
            }
        }

        if (Plugin.WantedSystemActive)
        {
            var heatData = Cache.heatCache[user.PlatformId];
            foreach (var (faction, heat) in heatData.heat)
            {
                SendWantedData(user, faction, heat.level);
            }
        }
    }

    private static string XpColour = "#ffcc33";
    private static string MasteryColour = "#ccff33";
    private static string BloodMasteryColour = "#cc0000";

    public static void SendXpData(User user, int level, float progressPercent, int earned, int needed, int change)
    {
        var changeText = change == 0 ? "" : $"{change:+##.###;-##.###;0}";
        var activeState = change > 0 ? ActiveState.Burst : ActiveState.Active;
        XPShared.Transport.Utils.ServerSetBarData(user, "XPRising.XP", "XP", level, progressPercent, $"XP: {earned}/{needed}", activeState, XpColour, changeText);
    }

    public static void SendMasteryData(User user, GlobalMasterySystem.MasteryType type, float mastery,
        ActiveState activeState, float changeInMastery = 0)
    {
        var colour = GlobalMasterySystem.GetMasteryCategory(type) == GlobalMasterySystem.MasteryCategory.Blood
            ? BloodMasteryColour
            : MasteryColour;
        var changeText = changeInMastery == 0 ? "" : $"{changeInMastery:+##.###;-##.###;0}";
        XPShared.Transport.Utils.ServerSetBarData(user, $"XPRising.{GlobalMasterySystem.GetMasteryCategory(type)}", $"{type}", (int)mastery, mastery*0.01f, $"{type} mastery", activeState, colour, changeText);
    }

    public static void SendWantedData(User user, Faction faction, int heat)
    {
        var heatIndex = FactionHeat.GetWantedLevel(heat);
        var baseHeat = heatIndex > 0 ? FactionHeat.HeatLevels[heatIndex - 1] : 0;
        var percentage = (float)(heat - baseHeat) / FactionHeat.HeatLevels[heatIndex];
        var activeState = heat > 0 ? ActiveState.Active : ActiveState.NotActive;
        var colour1 = heatIndex > 0 ? FactionHeat.ColourGradient[heatIndex - 1] : "white";
        var colour2 = FactionHeat.ColourGradient[heatIndex];
        XPShared.Transport.Utils.ServerSetBarData(user, "XPRising.heat", $"{faction}", heatIndex, percentage, $"Faction {faction}", activeState, $"@{colour1}@{colour2}");
    }
    
    private static readonly Dictionary<ulong, FrameTimer> FrameTimers = new();
    public static void SendPlayerDataOnDelay(User userData)
    {
        // If there is an existing timer, restart that
        if (FrameTimers.TryGetValue(userData.PlatformId, out var timer))
        {
            timer.Start();
        }
        else
        {
            // Create a new timer that fires once after 100ms 
            var newTimer = new FrameTimer();
            newTimer.Initialise(() =>
            {
                // Update the UI
                SendPlayerData(userData);
                // Remove the timer and dispose of it
                if (FrameTimers.Remove(userData.PlatformId, out timer)) timer.Stop();
            }, TimeSpan.FromMilliseconds(200), true).Start();
            
            FrameTimers.Add(userData.PlatformId, newTimer);
        }
    }

    private static void SendActionData(User user)
    {
        var userUiBarPreference = Database.PlayerPreferences[user.PlatformId].UIProgressDisplay;

        string currentMode;
        switch (userUiBarPreference)
        {
            case Actions.BarState.None:
            default:
                currentMode = "None";
                break;
            case Actions.BarState.Active:
                currentMode = "Active";
                break;
            case Actions.BarState.All:
                currentMode = "All";
                break;
        }
        
        XPShared.Transport.Utils.ServerSetAction(user, "XPRising.action", BarToggleAction, $"Toggle mastery [{currentMode}]");
    }
}