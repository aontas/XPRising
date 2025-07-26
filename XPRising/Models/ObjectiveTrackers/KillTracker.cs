using BepInEx.Logging;
using ProjectM;
using ProjectM.Network;
using XPRising.Systems;
using XPRising.Transport;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using XPShared;
using XPShared.Events;
using Faction = XPRising.Utils.Prefabs.Faction;

namespace XPRising.Models.ObjectiveTrackers;

public class KillObjectiveTracker : IObjectiveTracker
{
    public int StageIndex { get; }
    public int Index { get; }
    public string Objective { get; private set; }
    public float Progress { get; private set; }
    public State Status { get; private set; }
    public TimeSpan TimeTaken { get; private set; }
    public bool AddsStageProgress => _killsRequired > 0;
    // Score is reported as 0 when tracking towards a limit (i.e. pass/fail), otherwise it reports the kill count
    public float Score => _killsRequired > 0 ? 0 : _killCount;

    private readonly string _challengeId;
    private readonly ulong _steamId;
    private readonly float _killsRequired; // Using float so we can don't get loss of fraction when calculating progress
    private readonly List<Faction> _factions;
    private readonly List<BloodType> _bloodTypes;
    private readonly string _targetsTooltip; // Describes which factions/units should be targeted
    private readonly Action<ServerEvents.CombatEvents.PlayerKillMob> _handler;
    private int _killCount;
    private DateTime _startTime = DateTime.MinValue;

    public KillObjectiveTracker(string challengeId, ulong steamId, int index, int stageIndex, int killCount, List<Faction> factions, List<BloodType> bloodTypes)
    {
        _challengeId = challengeId;
        _steamId = steamId;
        StageIndex = stageIndex;
        Index = index;
        _killsRequired = killCount;
        
        _factions = ValidateFactions(factions);
        if (_factions.Count > 0)
        {
            var userPreferences = Database.PlayerPreferences[steamId];
            // Convert the list of factions 
            _targetsTooltip = $" ({string.Join(",", _factions.Select(faction => ClientActionHandler.FactionTooltip(faction, userPreferences.Language)).OrderBy(x => x))})";
        }
        
        _bloodTypes = ValidateBloodTypes(bloodTypes);
        if (_bloodTypes.Count > 0)
        {
            var userPreferences = Database.PlayerPreferences[steamId];
            // Convert the list of factions 
            _targetsTooltip = $" ({string.Join(",", _bloodTypes.Select(type => {
                    var message = type switch
                    {
                        BloodType.Brute => L10N.Get(L10N.TemplateKey.BarBloodBrute),
                        BloodType.Corruption => L10N.Get(L10N.TemplateKey.BarBloodCorruption),
                        BloodType.Creature => L10N.Get(L10N.TemplateKey.BarBloodCreature),
                        BloodType.Draculin => L10N.Get(L10N.TemplateKey.BarBloodDraculin),
                        BloodType.Mutant => L10N.Get(L10N.TemplateKey.BarBloodMutant),
                        BloodType.Rogue => L10N.Get(L10N.TemplateKey.BarBloodRogue),
                        BloodType.Scholar => L10N.Get(L10N.TemplateKey.BarBloodScholar),
                        BloodType.VBlood => L10N.Get(L10N.TemplateKey.BloodVBlood),
                        BloodType.Warrior => L10N.Get(L10N.TemplateKey.BarBloodWarrior),
                        BloodType.Worker => L10N.Get(L10N.TemplateKey.BarBloodWorker),
                        // Note: All other blood types will hit default but this shouldn't happen as we have normalised it above
                        _ => new L10N.LocalisableString("Unknown")
                    };
                    return message.Build(userPreferences.Language);
                }
            ).OrderBy(x => x))})";
        }

        if (killCount > 0)
        {
            Objective = $"Kill: {killCount} mobs{_targetsTooltip}";
        }
        else
        {
            Objective = $"Kill!{_targetsTooltip}";
            Progress = -1f;
        }
        Status = State.NotStarted;
        TimeTaken = TimeSpan.Zero;

        _handler = this.TrackKill;
    }

    public void Start()
    {
        Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"kill tracker start: {StageIndex}-{Index}");
        // Create the appropriate subscriptions to ensure we can update our state
        VEvents.ModuleRegistry.Subscribe(_handler);
        if (!AddsStageProgress)
        {
            Status = State.Complete;
        }
        else if (Status == State.NotStarted)
        {
            Status = State.InProgress;
        }
        _startTime = DateTime.Now;
    }

    public void Stop(State endState)
    {
        // Clean up any subscriptions
        VEvents.ModuleRegistry.Unsubscribe(_handler);
        Status = endState;
        Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"kill tracker stop: {StageIndex}-{Index}");
        // If this is the limit version, then record how long it took to reach that limit
        if (_killsRequired > 0)
        {
            TimeTaken += (DateTime.Now - _startTime);
        }
    }

    private void TrackKill(ServerEvents.CombatEvents.PlayerKillMob e)
    {
        var userEntity = e.Source.Read<PlayerCharacter>().UserEntity;
        var killerUserComponent = userEntity.Read<User>();
        if (killerUserComponent.PlatformId != _steamId) return;

        if (_factions.Count > 0)
        {
            if (!e.Target.HasValue)
            {
                Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Warning, () => $"Player killed entity but target not set");
                return;
            }
            if (!e.Target.Value.TryGetComponent<FactionReference>(out var victimFactionReference))
            {
                Plugin.Log(Plugin.LogSystem.Faction, LogLevel.Warning, () => $"Player killed: Entity: {e.Target.Value}, but it has no faction");
                return;
            }
            
            // Validate the faction is one we want
            var victimFaction = victimFactionReference.FactionGuid._Value;
            FactionHeat.GetActiveFaction(victimFaction, out var activeFaction);
            if (!_factions.Contains(activeFaction)) return;
        }
        if (_bloodTypes.Count > 0)
        {
            if (!e.Target.HasValue)
            {
                Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Warning, () => $"Player killed entity but target not set");
                return;
            }

            var (bloodType, _, isVBlood) = Helper.GetBloodInfo(e.Target.Value);
            var isValidBlood = isVBlood && _bloodTypes.Contains(BloodType.VBlood) || _bloodTypes.Contains(bloodType);
            if (!isValidBlood) return;
        }

        _killCount++;
        if (_killsRequired > 0)
        {
            Progress = Math.Min(_killCount / _killsRequired, 1.0f);
            if (Progress >= 1.0f)
            {
                Stop(State.Complete);
            }
        }
        else
        {
            Objective = $"Kill! x{_killCount}{_targetsTooltip}";
        }
        
        Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"Tracking kill: {_killCount}/{_killsRequired:F0} ({Progress*100:F1}%)");
        ChallengeSystem.UpdateChallenge(_challengeId, _steamId);
    }

    private static List<Faction> ValidateFactions(List<Faction> factions)
    {
        if (factions == null) return new List<Faction>();
        // make sure we match wanted system for internal consistency
        return factions.Select((faction) =>
            {
                FactionHeat.GetActiveFaction(faction, out var activeFaction);
                if (activeFaction == Faction.Unknown)
                {
                    Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Warning, () => $"Faction not currently supported for objectives: {faction}");
                }
                return activeFaction;
            })
            // Remove unknown factions (can add support for them into FactionHeat later, even if not exposed to WantedSystem as "active" factions)
            .Where(faction => faction != Faction.Unknown)
            // Get unique factions
            .Distinct().ToList();
    }

    private static List<BloodType> ValidateBloodTypes(List<BloodType> bloodTypes)
    {
        if (bloodTypes == null) return new List<BloodType>();
        return bloodTypes
            .Select(bloodType =>
            {
                return bloodType switch
                {
                    // GateBoss is really just VBlood (at this stage)
                    BloodType.DraculaTheImmortal => BloodType.VBlood,
                    BloodType.GateBoss => BloodType.VBlood,
                    // Unknown maps to none
                    BloodType.Unknown => BloodType.None,
                    // other types return as they are
                    _ => bloodType
                };
            })
            // Remove unknown blood types
            .Where(bloodType => bloodType != BloodType.None)
            // Get unique blood types
            .Distinct().ToList();
    }
}