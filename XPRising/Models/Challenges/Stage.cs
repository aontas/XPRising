using XPRising.Models.ObjectiveTrackers;

namespace XPRising.Models.Challenges;

public class Stage
{
    public int Index { get; private set; }
    public List<IObjectiveTracker> Objectives { get; private set; }
    
    public Stage(int index, List<IObjectiveTracker> objectives)
    {
        Index = index;
        Objectives = objectives;
    }

    public bool CalculateScore(out TimeSpan timeTaken, out float score)
    {
        timeTaken = TimeSpan.Zero;
        score = 0;
        var scoreValid = true;

        foreach (var objective in Objectives)
        {
            switch (objective.Status)
            {
                case State.NotStarted:
                    // skip this objective if not started
                    continue;
                case State.Failed:
                    // Score not valid
                    scoreValid = false;
                    timeTaken = TimeSpan.Zero;
                    score = 0;
                    break;
                case State.InProgress:
                    // Score is added
                    score += objective.Score;
                    // time is ignored as this is not yet complete
                    break;
                case State.Complete:
                case State.ChallengeComplete:
                    // Score is added
                    score += objective.Score;
                    // time uses max
                    timeTaken = new TimeSpan(Math.Max(timeTaken.Ticks, objective.TimeTaken.Ticks));
                    break;
            }
        }
        
        return scoreValid;
    }

    /// <summary>
    /// Fails any outstanding objective trackers to mark this stage as failed
    /// </summary>
    public void Fail()
    {
        Objectives.ForEach(objective => objective.Stop(State.Failed));
    }

    public State CurrentState()
    {
        if (Objectives.Count == 0) return State.Complete;

        var status = Objectives[0].Status;
        foreach (var objective in Objectives)
        {
            // // Limit objectives are only relevant when they get listed as failed 
            // if (objective.IsLimit && objective.Status != State.Failed) continue;
            
            switch (objective.Status)
            {
                case State.NotStarted:
                    // Any other state is more important than this one, so it will not replace the status
                    break;
                case State.InProgress:
                    // Always set status as in progress if we hit that
                    status = State.InProgress;
                    break;
                case State.Failed:
                    // Immediately return if some objective has failed
                    return State.Failed;
                case State.Complete:
                    // Do nothing. Either we match and nothing changes or the main status does not match, so we keep that.
                    break;
            }
        }

        return status;
    }

    public float CurrentProgress()
    {
        // "Limit" objectives should not be counted for progress
        var objectivesWithProgress = Objectives
            .Where(objective => objective.AddsStageProgress && objective.Progress >= 0)
            .Select(objective => objective.Progress)
            .ToList();
        return objectivesWithProgress.Any() ? objectivesWithProgress.Average() : -1f;
    }
}