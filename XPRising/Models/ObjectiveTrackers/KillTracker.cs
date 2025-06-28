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
    private readonly Action<ServerEvents.CombatEvents.PlayerKillMob> _handler;
    private int _killCount;
    private DateTime _startTime = DateTime.MinValue;

    public KillObjectiveTracker(string challengeId, ulong steamId, int index, int stageIndex, int killCount)
    {
        _challengeId = challengeId;
        _steamId = steamId;
        StageIndex = stageIndex;
        Index = index;
        _killsRequired = killCount;

        if (killCount > 0)
        {
            Objective = $"Kill: {killCount} mobs";
        }
        else
        {
            Objective = "Kill!";
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
        if (killerUserComponent.PlatformId == _steamId)
        {
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
                Objective = $"Kill! x{_killCount}";
            }
        }
        Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"Tracking kill: {_killCount}/{_killsRequired:F0} ({Progress*100:F1}%)");
        ChallengeSystem.UpdateChallenge(_challengeId, _steamId);
    }
}