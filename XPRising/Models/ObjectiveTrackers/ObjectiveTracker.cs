namespace XPRising.Models.ObjectiveTrackers;

public enum State
{
    NotStarted,
    InProgress,
    Failed,
    Complete,
    ChallengeComplete
}

public static class StateExtensions
{
    public static bool IsFinished(this State status)
    {
        return status == State.Failed || status == State.ChallengeComplete;
    }
}

public interface IObjectiveTracker
{
    public int StageIndex { get; }
    public int Index { get; }
    public string Objective { get; }
    public float Progress { get; }
    public State Status { get; }
    public bool AddsStageProgress => false;
    public TimeSpan TimeTaken => TimeSpan.Zero;
    public float Score => 0;

    // Functions to start or stop the objective
    public abstract void Start();
    public abstract void Stop(State endState);
}

public class InvalidObjective : IObjectiveTracker
{
    public int StageIndex { get; }
    public int Index { get; }
    public string Objective => "Invalid objective";
    public float Progress => 0;
    public State Status => State.Failed;
    public void Start() {}
    public void Stop(State endState) {}

    public InvalidObjective(int index, int stageIndex)
    {
        StageIndex = stageIndex;
        Index = index;
    }
}

public class CancelledObjective : IObjectiveTracker
{
    public int StageIndex { get; }
    public int Index { get; }
    public string Objective => "Cancelled";
    public float Progress => 0;
    public State Status => State.Failed;
    public void Start() {}
    public void Stop(State endState) {}
    
    public CancelledObjective(int index, int stageIndex)
    {
        StageIndex = stageIndex;
        Index = index;
    }
}