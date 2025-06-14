namespace XPRising.Models.ObjectiveTrackers;

public enum State
{
    NotStarted,
    InProgress,
    Failed,
    StageComplete,
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
    public string Objective { get; }
    public float Progress { get; }
    public State Status { get; }

    // Functions to start or stop the objective
    public abstract void Start();
    public abstract void Stop(State endState);
}

public struct InvalidObjective : IObjectiveTracker
{
    public string Objective => "Invalid objective";
    public float Progress => 0;
    public State Status => State.Failed;
    public void Start() {}
    public void Stop(State endState) {}
}

public struct CancelledObjective : IObjectiveTracker
{
    public string Objective => "Cancelled";
    public float Progress => 0;
    public State Status => State.Failed;
    public void Start() {}
    public void Stop(State endState) {}
}