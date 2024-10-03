using BepInEx.Logging;
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
        Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, $"UI Message: {user.PlatformId}: {action.Action}");
        var sendPlayerData = false;
        var sendActionData = false;
        switch (action.Action)
        {
            case ClientAction.ActionType.Connect:
                sendPlayerData = true;
                sendActionData = true;
                Cache.PlayerClientUICache[user.PlatformId] = true;
                // Send acknowledgement of connection
                MessageHandler.ServerSendToClient(user, new ConnectedMessage());
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

        SendUIData(user, sendPlayerData, sendActionData);
    }

    public static void SendUIData(User user, bool sendPlayerData, bool sendActionData)
    {
        // Only send UI data if the player is online and have connected with the UI.
        if (!PlayerCache.IsPlayerOnline(user.PlatformId) || !Cache.PlayerClientUICache[user.PlatformId]) return;
        
        if (sendPlayerData) SendPlayerData(user);
        if (sendActionData) SendActionData(user);
    }

    private static void SendPlayerData(User user)
    {
        var preferences = Database.PlayerPreferences[user.PlatformId];
        var userUiBarPreference = preferences.UIProgressDisplay;
        
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
                SendMasteryData(user, masteryType, (float)mastery.Mastery, preferences.Language, setActive ? ActiveState.Active : ActiveState.NotActive);
            }
        }
        else
        {
            SendMasteryData(user, GlobalMasterySystem.MasteryType.None, 0, preferences.Language, ActiveState.NotActive);
        }

        if (Plugin.WantedSystemActive)
        {
            var heatData = Database.PlayerHeat[user.PlatformId];
            if (heatData.heat.Count > 0)
            {
                foreach (var (faction, heat) in heatData.heat)
                {
                    SendWantedData(user, faction, heat.level, preferences.Language);
                }
            }
            else
            {
                // Send a bar for this group to ensure the UI is in a good state.
                SendWantedData(user, Faction.Critters, 0, preferences.Language);
            }
        }
        else
        {
            // Send a bar for this group to ensure the UI is in a good state.
            SendWantedData(user, Faction.Critters, 0, preferences.Language);
        }
    }

    public static void SendActiveBloodMasteryData(User user, GlobalMasterySystem.MasteryType activeBloodType)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[user.PlatformId]) return;
        var userPreferences = Database.PlayerPreferences[user.PlatformId];
        if (!Plugin.BloodlineSystemActive ||
            userPreferences.UIProgressDisplay != Actions.BarState.Active) return;
        
        var masteryData = Database.PlayerMastery[user.PlatformId];
        var newMasteryData = masteryData.TryGetValue(activeBloodType, out var mastery) ? (float)mastery.Mastery : 0;
        SendMasteryData(user, activeBloodType, newMasteryData, userPreferences.Language, ActiveState.OnlyActive);
    }

    public static string MasteryTooltip(GlobalMasterySystem.MasteryType type, string language)
    {
        var message = type switch
        {
            GlobalMasterySystem.MasteryType.WeaponUnarmed => L10N.Get(L10N.TemplateKey.BarWeaponUnarmed),
            GlobalMasterySystem.MasteryType.WeaponSpear => L10N.Get(L10N.TemplateKey.BarWeaponSpear),
            GlobalMasterySystem.MasteryType.WeaponSword => L10N.Get(L10N.TemplateKey.BarWeaponSword),
            GlobalMasterySystem.MasteryType.WeaponScythe => L10N.Get(L10N.TemplateKey.BarWeaponScythe),
            GlobalMasterySystem.MasteryType.WeaponCrossbow => L10N.Get(L10N.TemplateKey.BarWeaponCrossbow),
            GlobalMasterySystem.MasteryType.WeaponMace => L10N.Get(L10N.TemplateKey.BarWeaponMace),
            GlobalMasterySystem.MasteryType.WeaponSlasher => L10N.Get(L10N.TemplateKey.BarWeaponSlasher),
            GlobalMasterySystem.MasteryType.WeaponAxe => L10N.Get(L10N.TemplateKey.BarWeaponAxe),
            GlobalMasterySystem.MasteryType.WeaponFishingPole => L10N.Get(L10N.TemplateKey.BarWeaponFishingPole),
            GlobalMasterySystem.MasteryType.WeaponRapier => L10N.Get(L10N.TemplateKey.BarWeaponRapier),
            GlobalMasterySystem.MasteryType.WeaponPistol => L10N.Get(L10N.TemplateKey.BarWeaponPistol),
            GlobalMasterySystem.MasteryType.WeaponGreatSword => L10N.Get(L10N.TemplateKey.BarWeaponGreatSword),
            GlobalMasterySystem.MasteryType.WeaponLongBow => L10N.Get(L10N.TemplateKey.BarWeaponLongBow),
            GlobalMasterySystem.MasteryType.WeaponWhip => L10N.Get(L10N.TemplateKey.BarWeaponWhip),
            GlobalMasterySystem.MasteryType.Spell => L10N.Get(L10N.TemplateKey.BarSpell),
            GlobalMasterySystem.MasteryType.BloodNone => L10N.Get(L10N.TemplateKey.BarBloodNone),
            GlobalMasterySystem.MasteryType.BloodBrute => L10N.Get(L10N.TemplateKey.BarBloodBrute),
            GlobalMasterySystem.MasteryType.BloodCreature => L10N.Get(L10N.TemplateKey.BarBloodCreature),
            GlobalMasterySystem.MasteryType.BloodDracula => L10N.Get(L10N.TemplateKey.BarBloodDracula),
            GlobalMasterySystem.MasteryType.BloodDraculin => L10N.Get(L10N.TemplateKey.BarBloodDraculin),
            GlobalMasterySystem.MasteryType.BloodMutant => L10N.Get(L10N.TemplateKey.BarBloodMutant),
            GlobalMasterySystem.MasteryType.BloodRogue => L10N.Get(L10N.TemplateKey.BarBloodRogue),
            GlobalMasterySystem.MasteryType.BloodScholar => L10N.Get(L10N.TemplateKey.BarBloodScholar),
            GlobalMasterySystem.MasteryType.BloodWarrior => L10N.Get(L10N.TemplateKey.BarBloodWarrior),
            GlobalMasterySystem.MasteryType.BloodWorker => L10N.Get(L10N.TemplateKey.BarBloodWorker),
            // Note: GlobalMasterySystem.MasteryType.None will also hit default, but there should be no bar for this.
            _ => new L10N.LocalisableString("Unknown")
        };

        return message.Build(language);
    }
    
    public static string FactionTooltip(Faction type, string language)
    {
        var message = type switch
        {
            Faction.Bandits => L10N.Get(L10N.TemplateKey.BarFactionBandits),
            Faction.Critters => L10N.Get(L10N.TemplateKey.BarFactionCritters),
            Faction.Gloomrot => L10N.Get(L10N.TemplateKey.BarFactionGloomrot),
            Faction.Legion => L10N.Get(L10N.TemplateKey.BarFactionLegion),
            Faction.Militia => L10N.Get(L10N.TemplateKey.BarFactionMilitia),
            Faction.Undead => L10N.Get(L10N.TemplateKey.BarFactionUndead),
            Faction.Werewolf => L10N.Get(L10N.TemplateKey.BarFactionWerewolf),
            // Note: All other factions will hit default, but there should be no bar for these.
            _ => new L10N.LocalisableString("Unknown")
        };

        return message.Build(language);
    }

    private static string XpColour = "#ffcc33";
    private static string MasteryColour = "#ccff33";
    private static string BloodMasteryColour = "#cc0000";

    public static void SendXpData(User user, int level, float progressPercent, int earned, int needed, int change)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[user.PlatformId]) return;
        var preferences = Database.PlayerPreferences[user.PlatformId];
        var tooltip =
            L10N.Get(L10N.TemplateKey.BarXp)
                .AddField("{earned}", $"{earned}")
                .AddField("{needed}", $"{needed}")
                .Build(preferences.Language);
        
        var changeText = change == 0 ? "" : $"{change:+##.###;-##.###;0}";
        XPShared.Transport.Utils.ServerSetBarData(user, "XPRising.XP", "XP", $"{level:D2}", progressPercent, tooltip, ActiveState.Active, XpColour, changeText);
    }
    
    public static void SendMasteryData(User user, GlobalMasterySystem.MasteryType type, float mastery, string userLanguage,
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
            Tooltip = MasteryTooltip(type, userLanguage),
            Active = activeState,
            Colour = colour,
            Change = changeText,
            Flash = changeInMastery != 0
        };
        MessageHandler.ServerSendToClient(user, msg);
    }

    public static void SendWantedData(User user, Faction faction, int heat, string userLanguage)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[user.PlatformId]) return;
        
        var heatIndex = FactionHeat.GetWantedLevel(heat);
        var percentage = 1f;
        var colourString = "";
        var activeState = ActiveState.Active;
        if (heatIndex == FactionHeat.HeatLevels.Length)
        {
            colourString = $"#{FactionHeat.ColourGradient[heatIndex - 1]}";
        }
        else
        {
            var atMaxHeat = heatIndex == FactionHeat.HeatLevels.Length;
            var baseHeat = heatIndex > 0 ? FactionHeat.HeatLevels[heatIndex - 1] : 0;
            percentage = atMaxHeat ? 1 : (float)(heat - baseHeat) / (FactionHeat.HeatLevels[heatIndex] - baseHeat);
            activeState = heat > 0 ? ActiveState.Active : ActiveState.NotActive;
            var colour1 = heatIndex > 0 ? $"#{FactionHeat.ColourGradient[heatIndex - 1]}" : "white";
            var colour2 = atMaxHeat ? colour1 : $"#{FactionHeat.ColourGradient[heatIndex]}";
            colourString = $"@{colour1}@{colour2}";
        }
        
        XPShared.Transport.Utils.ServerSetBarData(user, "XPRising.heat", $"{faction}", $"{heatIndex:D}★", percentage, FactionTooltip(faction, userLanguage), activeState, colourString);
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