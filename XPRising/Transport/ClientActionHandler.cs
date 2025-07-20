using BepInEx.Logging;
using ProjectM.Network;
using XPRising.Commands;
using XPRising.Models;
using XPRising.Models.Challenges;
using XPRising.Models.ObjectiveTrackers;
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

    public static void InternalRegisterClient(ulong steamId, User user)
    {
        // Enable UI for client
        Cache.PlayerClientUICache[steamId] = true;
        
        // Send acknowledgement of connection
        MessageHandler.ServerSendToClient(user, new ConnectedMessage());
    }
    
    public static void HandleClientRegistered(ulong steamId)
    {
        var player = Cache.SteamPlayerCache[steamId];
        var user = player.UserEntity.GetUser();
        
        InternalRegisterClient(steamId, user);
        var preferences = Database.PlayerPreferences[user.PlatformId];
        SendUIData(user, true, true, preferences);
    }
    
    private const string BarToggleAction = "XPRising.BarMode";
    private const string DisplayBuffsAction = "XPRising.DisplayBuffs";
    public static void HandleClientAction(User user, ClientAction action)
    {
        var preferences = Database.PlayerPreferences[user.PlatformId];
        Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, $"UI Message: {user.PlatformId}: {action.Action}");
        var sendPlayerData = false;
        var sendActionData = false;
        switch (action.Action)
        {
            case ClientAction.ActionType.Connect:
                sendPlayerData = true;
                sendActionData = true;
                InternalRegisterClient(user.PlatformId, user);
                break;
            case ClientAction.ActionType.ButtonClick:
                switch (action.Value)
                {
                    case BarToggleAction:
                        Actions.BarStateChanged(user);
                        sendPlayerData = true;
                        sendActionData = true;
                        break;
                    case DisplayBuffsAction:
                        Cache.SteamPlayerCache.TryGetValue(user.PlatformId, out var playerData);
                        var messages = new List<L10N.LocalisableString>();
                        PlayerInfoCommands.GenerateBuffStatus(playerData, ref messages);
                        var stringMessages = messages.Select(message => message.Build(preferences.Language)).ToList();
                        XPShared.Transport.Utils.ServerSendText(user, "XPRising.BuffText", "XPRising.BuffText",  L10N.Get(L10N.TemplateKey.PlayerInfoBuffs).Build(preferences.Language), stringMessages);
                        break;
                    default:
                        sendActionData = true;
                        ChallengeSystem.ToggleChallenge(user.PlatformId, action.Value);
                        break;
                }
                break;
            case ClientAction.ActionType.Disconnect:
            default:
                // Do nothing
                break;
        }

        SendUIData(user, sendPlayerData, sendActionData, preferences);
    }

    public static void SendUIData(User user, bool sendPlayerData, bool sendActionData, PlayerPreferences preferences)
    {
        // Only send UI data if the player is online and have connected with the UI.
        if (!PlayerCache.IsPlayerOnline(user.PlatformId) || !Cache.PlayerClientUICache[user.PlatformId]) return;
        
        if (sendPlayerData) SendPlayerData(user, preferences);
        if (sendActionData) SendActionData(user, preferences);
    }

    private static void SendPlayerData(User user, PlayerPreferences preferences)
    {
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
                    activeWeaponMastery == GlobalMasterySystem.MasteryType.None)
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
                SendMasteryData(user, masteryType, (float)mastery.Mastery, (float)mastery.Effectiveness, preferences.Language, setActive ? ActiveState.Active : ActiveState.NotActive);
            }
        }
        else
        {
            SendMasteryData(user, GlobalMasterySystem.MasteryType.None, 0, 1, preferences.Language, ActiveState.NotActive);
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

        if (Plugin.ChallengeSystemActive)
        {
            SendChallengeData(user);
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
        SendMasteryData(user, activeBloodType, newMasteryData, (float)mastery.Effectiveness, userPreferences.Language, ActiveState.OnlyActive);
    }

    public static string MasteryTooltip(GlobalMasterySystem.MasteryType type, string language)
    {
        var message = type switch
        {
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
            GlobalMasterySystem.MasteryType.WeaponDaggers => L10N.Get(L10N.TemplateKey.BarWeaponDaggers),
            GlobalMasterySystem.MasteryType.WeaponClaws => L10N.Get(L10N.TemplateKey.BarWeaponClaws),
            GlobalMasterySystem.MasteryType.WeaponTwinblades => L10N.Get(L10N.TemplateKey.BarWeaponTwinBlades),
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
            GlobalMasterySystem.MasteryType.BloodCorruption => L10N.Get(L10N.TemplateKey.BarBloodCorruption),
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
            Faction.Blackfangs => L10N.Get(L10N.TemplateKey.BarFactionBlackFangs),
            Faction.Corrupted => L10N.Get(L10N.TemplateKey.BarFactionCorrupted),
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

    public static void SendXpData(User user, int level, float progressPercent, int earned, int needed, int change)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[user.PlatformId]) return;
        var preferences = Database.PlayerPreferences[user.PlatformId];
        var tooltip = level == ExperienceSystem.MaxLevel ? L10N.Get(L10N.TemplateKey.BarXpMax).Build(preferences.Language) :
            L10N.Get(L10N.TemplateKey.BarXp)
                .AddField("{earned}", $"{earned}")
                .AddField("{needed}", $"{needed}")
                .Build(preferences.Language);
        var percentage = level == ExperienceSystem.MaxLevel ? -1f : progressPercent;
        
        var changeText = change == 0 ? "" : $"{change:+##.###;-##.###;0}";
        XPShared.Transport.Utils.ServerSetBarData(user, "XPRising.XP", "XP", $"{level:D2}", percentage, tooltip, ActiveState.Active, preferences.XpBarColour, changeText, change != 0);
    }
    
    public static void SendMasteryData(User user, GlobalMasterySystem.MasteryType type, float mastery, float effectiveness, string userLanguage,
        ActiveState activeState = ActiveState.Unchanged, float changeInMastery = 0)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[user.PlatformId]) return;
        var preferences = Database.PlayerPreferences[user.PlatformId];
        
        var colour = GlobalMasterySystem.GetMasteryCategory(type) == GlobalMasterySystem.MasteryCategory.Blood
            ? preferences.BloodMasteryBarColour
            : preferences.MasteryBarColour;
        
        var changeText = changeInMastery == 0 ? "" : $"{changeInMastery:+##.###;-##.###;0}";
        var msg = new ProgressSerialisedMessage()
        {
            Group = $"XPRising.{GlobalMasterySystem.GetMasteryCategory(type)}",
            Label = $"{type}",
            ProgressPercentage = mastery*0.01f,
            Header = $"{effectiveness:F1}x",
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
        var percentage = -1f;
        var colourString = "";
        var activeState = ActiveState.Active;
        var label = FactionTooltip(faction, userLanguage);
        
        var atMaxHeat = heatIndex == FactionHeat.HeatLevels.Length;
        if (atMaxHeat)
        {
            colourString = $"#{FactionHeat.MaxHeatColour}";
            label = $"{label} (+{heat-FactionHeat.LastHeatThreshold:D})";
        }
        else
        {
            var baseHeat = heatIndex > 0 ? FactionHeat.HeatLevels[heatIndex - 1] : 0;
            percentage = (float)(heat - baseHeat) / (FactionHeat.HeatLevels[heatIndex] - baseHeat);
            activeState = heat > 0 ? ActiveState.Active : ActiveState.NotActive;
            var colour1 = heatIndex > 0 ? $"#{FactionHeat.ColourGradient[heatIndex - 1]}" : "white";
            var colour2 = $"#{FactionHeat.ColourGradient[heatIndex]}";
            colourString = $"@{colour1}@{colour2}";
        }
        
        XPShared.Transport.Utils.ServerSetBarData(user, "XPRising.heat", $"{faction}", $"{heatIndex:D}★", percentage, label, activeState, colourString);
    }

    public static void SendChallengeUpdate(ulong steamId, string challengeId, ChallengeState state, bool remove = false)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[steamId] || !PlayerCache.FindPlayer(steamId, true, out _, out _, out var user)) return;
        
        var preferences = Database.PlayerPreferences[steamId];

        // Make this remove any out-of-date bars + set the current bars
        foreach (var stage in state.Stages)
        {
            var status = stage.CurrentState();
            var percentage = status == State.InProgress ? stage.CurrentProgress() : -1f;
            
            Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"{user.PlatformId} stage bars: {stage.Index} {status}");
            InternalSendChallengeBar(user, preferences, MakeBarId(challengeId, stage.Index, 0, true), status, $"S{stage.Index + 1:D}", "", percentage, false, remove);
            for (var i = 0; i < stage.Objectives.Count; i++)
            {
                var objective = stage.Objectives[i];
                // Only show the objective when the stage is in progress
                var showObjective = !remove && status == State.InProgress;
                // Update the progress
                InternalSendChallengeBar(user, preferences, MakeBarId(challengeId, stage.Index, i, false), objective.Status, $"--", objective.Objective, objective.Progress, true, !showObjective);
            }
        }
        
        // Update the button
        var challenge = ChallengeSystem.GetChallenge(challengeId);
        InternalSendChallengeButton(user, preferences, challenge, state.CurrentState());
    }
    
    public static void SendChallengeTimerUpdate(ulong steamId, string challengeId, IObjectiveTracker objective)
    {
        // Only send UI data to users if they have connected with the UI. 
        if (!Cache.PlayerClientUICache[steamId] || !PlayerCache.FindPlayer(steamId, true, out _, out _, out var user)) return;
        
        var preferences = Database.PlayerPreferences[steamId];

        // Update the display
        InternalSendChallengeBar(user, preferences, MakeBarId(challengeId, objective.StageIndex, objective.Index, false), objective.Status, $"--", objective.Objective, objective.Progress, false, false);
    }

    private static string MakeBarId(string challengeId, int stageIndex, int objectiveIndex, bool isStage)
    {
        return isStage ? $"{challengeId}-{stageIndex:D}" : $"{challengeId}-{stageIndex:D}-{objectiveIndex:D}";
    }
    
    private static void InternalSendChallengeBar(User user, PlayerPreferences preferences, string barId, State status, string header, string label, float percentage, bool flash, bool remove)
    {
        var activeState = ActiveState.Active;
        var colour = preferences.ChallengeActiveBarColour;
        L10N.LocalisableString message; 

        switch (status)
        {
            case State.Complete:
                message = L10N.Get(L10N.TemplateKey.ChallengeStageComplete);
                break;
            case State.ChallengeComplete:
                message = L10N.Get(L10N.TemplateKey.ChallengeComplete);
                break;
            case State.NotStarted:
                message = L10N.Get(L10N.TemplateKey.ChallengeInProgress);
                colour = preferences.ChallengeInactiveBarColour;
                percentage = -1;
                break;
            case State.Failed:
                message = L10N.Get(L10N.TemplateKey.ChallengeFailed);
                colour = preferences.ChallengeFailedBarColour;
                break;
            case State.InProgress:
                message = L10N.Get(L10N.TemplateKey.ChallengeInProgress);
                break;
            default:
                // Ignore other cases
                return;
        }

        // If this is a remove update, set the state to remove
        activeState = remove ? ActiveState.Remove : activeState;
        label = label == "" ? message.Build(preferences.Language) : label;
        XPShared.Transport.Utils.ServerSetBarData(user, "XPRising.challenges", barId, header, percentage, label, activeState, colour, "", flash);
    }

    private static void InternalSendChallengeButton(User user, PlayerPreferences preferences, ChallengeSystem.Challenge challenge, State status)
    {
        var label = challenge.Label;
        switch (status)
        {
            case State.NotStarted:
                break;
            case State.InProgress:
                label = $"<b>*{challenge.Label}*</b>";
                break;
            case State.Failed:
                label = challenge.CanRepeat ? label : $"<s>{challenge.Label}</s>";
                break;
            case State.Complete:
            case State.ChallengeComplete:
                label = challenge.CanRepeat ? label : $"{challenge.Label} [✓]";
                break;
        }
        XPShared.Transport.Utils.ServerSetAction(user, $"XPRising.challenges", challenge.ID, label);
        Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"{user.PlatformId} buttton: {label}");
    }

    private static void SendChallengeData(User user)
    {
        var preferences = Database.PlayerPreferences[user.PlatformId];

        // Only need the challenge data if we are using the challenge system
        if (Plugin.ChallengeSystemActive)
        {
            foreach (var (challenge, status) in ChallengeSystem.ListChallenges(user.PlatformId))
            {
                InternalSendChallengeButton(user, preferences, challenge, status);
            }
        }
    }

    private static void SendChallengeActions(User user)
    {
        var preferences = Database.PlayerPreferences[user.PlatformId];

        // Only need the challenge actions if we are using the challenge system
        if (Plugin.ChallengeSystemActive)
        {
            foreach (var (challenge, status) in ChallengeSystem.ListChallenges(user.PlatformId))
            {
                InternalSendChallengeButton(user, preferences, challenge, status);
            }
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
                var preferences = Database.PlayerPreferences[user.PlatformId];
                // Update the UI
                SendPlayerData(user, preferences);
                // Remove the timer and dispose of it
                if (FrameTimers.Remove(user.PlatformId, out timer)) timer.Stop();
            }, TimeSpan.FromMilliseconds(200), 1).Start();
            
            FrameTimers.Add(user.PlatformId, newTimer);
        }
    }

    private static void SendActionData(User user, PlayerPreferences preferences)
    {
        // Only need the mastery toggle switch if we are using a mastery mode
        if (Plugin.BloodlineSystemActive || Plugin.WeaponMasterySystemActive)
        {
            string currentMode;
            switch (preferences.UIProgressDisplay)
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

        if (Plugin.ShouldApplyBuffs)
        {
            XPShared.Transport.Utils.ServerSetAction(user, "XPRising.action", DisplayBuffsAction,
                $"Show buffs");
        }

        SendChallengeActions(user);
    }
}