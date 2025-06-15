using BepInEx.Logging;
using ProjectM.Behaviours;
using XPRising.Systems;
using XPRising.Transport;
using XPShared;

namespace XPRising.Models.ObjectiveTrackers;

public class TimeLimitTracker : IObjectiveTracker
{
    public int StageIndex { get; }
    public int Index { get; }
    public string Objective => $"Time limit ({FormatTimeSpan(TimeRemaining)})";
    public float Progress => (float)Math.Clamp(TimeRemaining.TotalSeconds / _limit.TotalSeconds, 0, 1);
    public State Status { get; private set; }
    public bool IsLimit => true;

    private TimeSpan TimeRemaining => _handler.Enabled ? _timeEnd < DateTime.Now ? TimeSpan.Zero : _timeEnd - DateTime.Now : _limit;
    
    private readonly string _challengeId;
    private readonly ulong _steamId;
    private readonly FrameTimer _handler;
    private readonly TimeSpan _limit;
    private DateTime _timeEnd;
    
    public TimeLimitTracker(string challengeId, ulong steamId, int index, int stageIndex, TimeSpan limit)
    {
        StageIndex = stageIndex;
        Index = index;
        _challengeId = challengeId;
        _steamId = steamId;

        Status = State.NotStarted;

        _handler = new FrameTimer();
        _limit = limit;
    }
    
    public void Start()
    {
        Status = State.InProgress;
        _timeEnd = DateTime.Now + _limit;
        _handler.Initialise(UpdateChallenge, TimeSpan.FromSeconds(1), -1);
        _handler.Start();
    }

    public void Stop(State endState)
    {
        _handler.Stop();
        Status = endState;
    }

    private void UpdateChallenge()
    {
        if (_timeEnd < DateTime.Now)
        {
            Stop(State.Failed);
            ChallengeSystem.UpdateChallenge(_challengeId, _steamId);
        }
        else
        {
            
            ClientActionHandler.SendChallengeTimerUpdate(_steamId, _challengeId, this);
        }
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        return ts.TotalHours > 0 ? $@"{ts.TotalHours:F0}{ts:mm\:ss}" : $@"{ts:mm\:ss}";
    }
}