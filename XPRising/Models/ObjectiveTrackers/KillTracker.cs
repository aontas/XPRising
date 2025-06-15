using BepInEx.Logging;
using ProjectM;
using ProjectM.Network;
using XPRising.Systems;
using XPShared;
using XPShared.Events;

namespace XPRising.Models.ObjectiveTrackers;

public class KillObjectiveTracker : IObjectiveTracker
{
    public int StageIndex { get; }
    public int Index { get; }
    public string Objective { get; }
    public float Progress { get; private set; }
    public State Status { get; private set; }

    private readonly string _challengeId;
    private readonly ulong _steamId;
    private readonly float _killsRequired; // Using float so we can don't get loss of fraction when calculating progress
    private readonly Action<ServerEvents.CombatEvents.PlayerKillMob> _handler;
    private int _killCount;

    public KillObjectiveTracker(string challengeId, ulong steamId, int index, int stageIndex, int killCount)
    {
        _challengeId = challengeId;
        _steamId = steamId;
        StageIndex = stageIndex;
        Index = index;
        _killsRequired = killCount;

        Objective = $"Kill {killCount} units";
        Status = State.NotStarted;

        _handler = this.TrackKill;
    }

    public void Start()
    {
        Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"kill tracker start", true);
        // Create the appropriate subscriptions to ensure we can update our state
        VEvents.ModuleRegistry.Subscribe(_handler);
        if (Status == State.NotStarted)
        {
            Status = State.InProgress;
        }
    }

    public void Stop(State endState)
    {
        // Clean up any subscriptions
        VEvents.ModuleRegistry.Unsubscribe(_handler);
        Status = endState;
    }

    private void TrackKill(ServerEvents.CombatEvents.PlayerKillMob e)
    {
        var userEntity = e.Source.Read<PlayerCharacter>().UserEntity;
        var killerUserComponent = userEntity.Read<User>();
        if (killerUserComponent.PlatformId == _steamId)
        {
            _killCount++;
            Progress = Math.Min(_killCount / _killsRequired, 1.0f);
            if (Progress >= 1.0f)
            {
                Stop(State.Complete);
            }
        }
        Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Warning, $"Tracking kill: {_killCount}/{_killsRequired:F0} ({Progress*100:F1}%)", true);
        ChallengeSystem.UpdateChallenge(_challengeId, _steamId);
    }
}