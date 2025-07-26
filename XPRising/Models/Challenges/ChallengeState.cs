using BepInEx.Logging;
using XPRising.Models.ObjectiveTrackers;

namespace XPRising.Models.Challenges;

public class ChallengeState
{
    public string ChallengeId;
    public List<Stage> Stages;
    public int ActiveStage { get; private set; }
    public DateTime StartTime = DateTime.Now;

    public State CurrentState()
    {
        if (ActiveStage >= Stages.Count) return State.ChallengeComplete;
        return Stages[ActiveStage].CurrentState();
    }

    public bool CalculateScore(out TimeSpan timeTaken, out int score)
    {
        timeTaken = TimeSpan.Zero;
        score = 0;
        var scoreValid = true;

        foreach (var stage in Stages)
        {
            if (stage.CalculateScore(out var stageTime, out var stageScore))
            {
                score += (int)stageScore;
                timeTaken = new TimeSpan(Math.Max(timeTaken.Ticks, stageTime.Ticks));
            }
            else
            {
                scoreValid = false;
            }
        }
        
        return scoreValid;
    }

    /// <summary>
    /// Marks this challenge as failed
    /// </summary>
    public void Fail()
    {
        // Active stage is invalid/there are no stages
        if (ActiveStage >= Stages.Count)
        {
            ActiveStage = Stages.Count;
            Stages.Add(new Stage(Stages.Count, new List<IObjectiveTracker>() { new CancelledObjective(0, 0) }));
            return;
        }

        // Mark the stage as failed
        Stages[ActiveStage].Fail();
    }

    public int UpdateStage(ulong steamId, out State currentState)
    {
        currentState = State.Complete;
        ActiveStage = 0;
    
        while (ActiveStage < Stages.Count && currentState == State.Complete)
        {
            var stage = Stages[ActiveStage];
            currentState = stage.CurrentState();
            switch (currentState)
            {
                case State.NotStarted:
                    // Start this stage
                    Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"Starting stage: {stage.Objectives.Count} objectives");
                    stage.Objectives.ForEach(objective => objective.Start());
                    currentState = State.InProgress;
                    break;
                case State.InProgress:
                    // This stage is in progress. Report the progress
                    break;
                case State.Failed:
                    // This stage has failed. Report the state
                    // Make sure we stop any outstanding objectives so they don't keep trying to track updates
                    stage.Objectives.ForEach(objective => objective.Stop(State.Failed));
                    break;
                case State.Complete:
                    // Completed, so we can go to next stage
                    ActiveStage++;
                    // Make sure we stop any limit objectives (such as the timer) so we don't keep ticking that down
                    stage.Objectives.ForEach(objective => objective.Stop(State.Complete));
                    break;
            }
        }

        if (ActiveStage == Stages.Count && currentState == State.Complete)
        {
            currentState = State.ChallengeComplete;
        }
        return ActiveStage;
    }
}