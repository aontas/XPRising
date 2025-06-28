using XPRising.Systems;
using XPRising.Transport;
using XPShared;

namespace XPRising.Models.ObjectiveTrackers;

public class TimeLimitTracker : IObjectiveTracker
{
    public int StageIndex { get; }
    public int Index { get; }
    public string Objective => $"Time limit ({FormatTimeSpan(TimeRemaining)})";
    public float Progress => _isCountDown ? (float)Math.Clamp(TimeRemaining.TotalSeconds / _span.TotalSeconds, 0, 1) : 1 - (float)Math.Clamp(TimeRemaining.TotalSeconds / _span.TotalSeconds, 0, 1);
    public State Status { get; private set; }
    // Score is currently always 0
    public float Score => 0;

    private TimeSpan TimeRemaining => _handler.Enabled ? _timeEnd < DateTime.Now ? TimeSpan.Zero : _timeEnd - DateTime.Now : _span;
    
    private readonly string _challengeId;
    private readonly ulong _steamId;
    private readonly FrameTimer _handler;
    private readonly TimeSpan _span;
    private DateTime _timeEnd;
    private bool _isCountDown;
    
    public TimeLimitTracker(string challengeId, ulong steamId, int index, int stageIndex, TimeSpan span)
    {
        StageIndex = stageIndex;
        Index = index;
        _challengeId = challengeId;
        _steamId = steamId;

        Status = State.NotStarted;

        _handler = new FrameTimer();
        _handler.Initialise(UpdateChallenge, TimeSpan.FromSeconds(1), -1);
        _span = span < TimeSpan.Zero ? -span : span;

        _isCountDown = span > TimeSpan.Zero;
    }
    
    public void Start()
    {
        Status = _isCountDown ? State.Complete : State.InProgress;
        _timeEnd = DateTime.Now + _span;
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
            var endState = _isCountDown ? State.Failed : State.Complete;
            Stop(endState);
            ChallengeSystem.UpdateChallenge(_challengeId, _steamId);
        }
        else
        {
            
            ClientActionHandler.SendChallengeTimerUpdate(_steamId, _challengeId, this);
        }
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        return ts.TotalHours >= 1 ? $@"{ts.TotalHours:F0}:{ts:mm\:ss}" : $@"{ts:mm\:ss}";
    }
}