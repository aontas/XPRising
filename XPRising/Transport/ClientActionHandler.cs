using ProjectM.Network;
using XPRising.Models;
using XPRising.Systems;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using XPShared;
using XPShared.Transport;
using XPShared.Transport.Messages;
using ActiveState = XPShared.Transport.Messages.ProgressSerialisedMessage.ActiveState;

namespace XPRising.Transport;

public static class ClientActionHandler
{
    private static readonly List<GlobalMasterySystem.MasteryType> DefaultMasteryList =
        Enum.GetValues<GlobalMasterySystem.MasteryType>().Where(type => type != GlobalMasterySystem.MasteryType.None).ToList();

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
                Cache.PlayerClientUICache[user.PlatformId] = true;
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
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[user.PlatformId]) return;
        
        var userUiBarPreference = Database.PlayerPreferences[user.PlatformId].UIProgressDisplay;
        
        if (Plugin.ExperienceSystemActive)
        {
            var xp = ExperienceSystem.GetXp(user.PlatformId);
            ExperienceSystem.GetLevelAndProgress(xp, out var level, out var progressPercent, out var earned, out var needed);
            SendXpData(user, level, progressPercent, earned, needed, 0);
        }

        if (Plugin.BloodlineSystemActive || Plugin.WeaponMasterySystemActive)
        {
            var markEmptyAsActive = false;
            var masteries = new List<GlobalMasterySystem.MasteryType>();
            if (userUiBarPreference == Actions.BarState.All)
            {
                masteries = DefaultMasteryList;
            }
            else if (userUiBarPreference == Actions.BarState.Active)
            {
                markEmptyAsActive = true;
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
            foreach (var masteryType in DefaultMasteryList)
            {
                var dataExists = true;
                if (!masteryData.TryGetValue(masteryType, out var mastery))
                {
                    mastery = new MasteryData();
                    dataExists = false;
                }
                var setActive = (dataExists || markEmptyAsActive) && masteries.Contains(masteryType);
                SendMasteryData(user, masteryType, (float)mastery.Mastery, setActive ? ActiveState.Active : ActiveState.NotActive);
            }
        }

        if (Plugin.WantedSystemActive)
        {
            var heatData = Database.PlayerHeat[user.PlatformId];
            foreach (var (faction, heat) in heatData.heat)
            {
                SendWantedData(user, faction, heat.level);
            }
        }
    }

    public static void SendActiveBloodMasteryData(User user, GlobalMasterySystem.MasteryType activeBloodType)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[user.PlatformId]) return;
        if (!Plugin.BloodlineSystemActive ||
            Database.PlayerPreferences[user.PlatformId].UIProgressDisplay != Actions.BarState.Active) return;
        
        var masteryData = Database.PlayerMastery[user.PlatformId];
        var newMasteryData = masteryData.TryGetValue(activeBloodType, out var mastery) ? (float)mastery.Mastery : 0;
        SendMasteryData(user, activeBloodType, newMasteryData, ActiveState.OnlyActive);
    }

    private static string XpColour = "#ffcc33";
    private static string MasteryColour = "#ccff33";
    private static string BloodMasteryColour = "#cc0000";

    public static void SendXpData(User user, int level, float progressPercent, int earned, int needed, int change)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[user.PlatformId]) return;
        
        var changeText = change == 0 ? "" : $"{change:+##.###;-##.###;0}";
        XPShared.Transport.Utils.ServerSetBarData(user, "XPRising.XP", "XP", $"{level:D2}", progressPercent, $"XP: {earned}/{needed}", ActiveState.Active, XpColour, changeText);
    }

    public static void SendMasteryData(User user, GlobalMasterySystem.MasteryType type, float mastery,
        ActiveState activeState = ActiveState.Unchanged, float changeInMastery = 0)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[user.PlatformId]) return;
        
        var colour = GlobalMasterySystem.GetMasteryCategory(type) == GlobalMasterySystem.MasteryCategory.Blood
            ? BloodMasteryColour
            : MasteryColour;
        var changeText = changeInMastery == 0 ? "" : $"{changeInMastery:+##.###;-##.###;0}";
        var msg = new ProgressSerialisedMessage()
        {
            Group = $"XPRising.{GlobalMasterySystem.GetMasteryCategory(type)}",
            Label = $"{type}",
            ProgressPercentage = mastery*0.01f,
            Header = $"{(int)mastery:D2}",
            Tooltip = $"{type} mastery",
            Active = activeState,
            Colour = colour,
            Change = changeText,
            Flash = changeInMastery != 0
        };
        MessageHandler.ServerSendToClient(user, msg);
    }

    public static void SendWantedData(User user, Faction faction, int heat)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[user.PlatformId]) return;
        
        var heatIndex = FactionHeat.GetWantedLevel(heat);
        if (heatIndex == FactionHeat.HeatLevels.Length)
        {
            XPShared.Transport.Utils.ServerSetBarData(user, "XPRising.heat", $"{faction}", $"{heatIndex:D}★", 1f, $"Faction {faction}", ActiveState.Active, $"#{FactionHeat.ColourGradient[heatIndex - 1]}");
        }
        else
        {
            var baseHeat = heatIndex > 0 ? FactionHeat.HeatLevels[heatIndex - 1] : 0;
            var percentage = (float)(heat - baseHeat) / (FactionHeat.HeatLevels[heatIndex] - baseHeat);
            var activeState = heat > 0 ? ActiveState.Active : ActiveState.NotActive;
            var colour1 = heatIndex > 0 ? $"#{FactionHeat.ColourGradient[heatIndex - 1]}" : "white";
            var colour2 = $"#{FactionHeat.ColourGradient[heatIndex]}";
            XPShared.Transport.Utils.ServerSetBarData(user, "XPRising.heat", $"{faction}", $"{heatIndex:D}★", percentage, $"Faction {faction}", activeState, $"@{colour1}@{colour2}");
        }
    }
    
    private static readonly Dictionary<ulong, FrameTimer> FrameTimers = new();
    public static void SendPlayerDataOnDelay(User user)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[user.PlatformId]) return;
        
        // If there is an existing timer, restart that
        if (FrameTimers.TryGetValue(user.PlatformId, out var timer))
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
                SendPlayerData(user);
                // Remove the timer and dispose of it
                if (FrameTimers.Remove(user.PlatformId, out timer)) timer.Stop();
            }, TimeSpan.FromMilliseconds(200), true).Start();
            
            FrameTimers.Add(user.PlatformId, newTimer);
        }
    }

    private static void SendActionData(User user)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[user.PlatformId]) return;
        
        var userUiBarPreference = Database.PlayerPreferences[user.PlatformId].UIProgressDisplay;

        // Only need the mastery toggle switch if we are using a mastery mode
        if (Plugin.BloodlineSystemActive || Plugin.WeaponMasterySystemActive)
        {
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

            XPShared.Transport.Utils.ServerSetAction(user, "XPRising.action", BarToggleAction,
                $"Toggle mastery [{currentMode}]");
        }
    }
}