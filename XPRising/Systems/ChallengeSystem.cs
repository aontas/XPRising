using System.Collections.ObjectModel;
using BepInEx.Logging;
using XPRising.Models;
using XPRising.Models.ObjectiveTrackers;
using XPRising.Transport;
using XPRising.Utils;
using XPRising.Utils.Prefabs;
using XPShared;
using Faction = XPRising.Utils.Prefabs.Faction;

namespace XPRising.Systems;

public static class ChallengeSystem
{
    public struct Objective
    {
        /*
         * Challenge options:
         * - time limit
         * - kill count
         * - damage count
         * - mob type
         * - mob faction
         * - mob blood type
         * - weapon use
         * - spell school use
         * - player blood type
         * - zone (eg, mortium vs farbane woods)
         * - location
         * - survive
         * - time of day (eg kills at night vs kills during day)
         * - level difference range?
         */
        public int killCount;
        public float damageCount;
        public List<Units> unitTypes;
        public List<Faction> factions;
        public List<GlobalMasterySystem.MasteryType> unitBloodType;
        public float bloodLevel;
        public List<GlobalMasterySystem.MasteryType> masteryTypes;
        public TimeSpan limit;
        public DateTime time;
        public int placement;
    }

    public class Stage
    {
        public int Index { get; private set; }
        public List<IObjectiveTracker> Objectives { get; private set; }
        
        public Stage(int index, List<IObjectiveTracker> objectives)
        {
            Index = index;
            Objectives = objectives;
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
            if (Objectives.Count == 0) return State.StageComplete;

            var status = Objectives[0].Status;
            foreach (var objective in Objectives)
            {
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
                    case State.StageComplete:
                        // Do nothing. Either we match and nothing changes or the main status does not match, so we keep that.
                        break;
                }
            }

            return status;
        }

        public float CurrentProgress()
        {
            return Objectives.Count == 0 ? 1f : Objectives.Select(objective => objective.Progress).Average();
        }
    }

    public struct Challenge
    {
        public string id;
        public string label;
        public List<List<Objective>> objectives;
        public bool canRepeat;
        // Reward?
    }

    public class ChallengeState
    {
        public string ChallengeId;
        public List<Stage> Stages;
        public int ActiveStage { get; private set; }

        public State CurrentState()
        {
            if (ActiveStage >= Stages.Count) return State.ChallengeComplete;
            return Stages[ActiveStage].CurrentState();
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
                Stages.Add(new Stage(Stages.Count, new List<IObjectiveTracker>() { new CancelledObjective() }));
                return;
            }

            // Mark the stage as failed
            Stages[ActiveStage].Fail();
        }

        public int UpdateStage(ulong steamId, out State currentState)
        {
            currentState = State.StageComplete;
            ActiveStage = 0;
        
            while (ActiveStage < Stages.Count && currentState == State.StageComplete)
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
                        break;
                    case State.StageComplete:
                        // Completed, so we can skip
                        ActiveStage++;
                        break;
                }
            }

            if (ActiveStage == Stages.Count && currentState == State.StageComplete)
            {
                currentState = State.ChallengeComplete;
            }
            return ActiveStage;
        }
    }

    public struct ChallengeStats
    {
        public DateTime firstCompleted;
        public DateTime lastCompleted;
        public int attempts;
        public int completeCount;
    }

    private static List<Challenge> _challenges;

    private static LazyDictionary<ulong, LazyDictionary<string, ChallengeState>> playerChallengeState = new();
    private static LazyDictionary<ulong, LazyDictionary<string, ChallengeStats>> playerChallengeStats = new();

    private struct ChallengeEnded
    {
        public string challengeId;
        public ulong steamId;
        public State endStatus;
        public DateTime removeTime;
    }
    
    // A list of challenges that have failed/completed so that we can remove them from the active list in the UI
    private static readonly List<ChallengeEnded> ChallengesToRemove = new();
    private static FrameTimer _removeTimer = new FrameTimer();
    
    public static bool IsPlayerLoggingChallenges(ulong steamId)
    {
        return Database.PlayerPreferences[steamId].LoggingChallenges;
    }

    public static void Initialise()
    {
        _challenges = testChallenges;
        if (Plugin.ChallengeSystemActive)
        {
            _removeTimer.Initialise(UpdateRemovedChallenges, TimeSpan.FromMilliseconds(500), -1);
            _removeTimer.Start();
        }
    }

    public static ReadOnlyCollection<(Challenge challenge, State status)> ListChallenges(ulong steamId, bool hideCompleted = true)
    {
        var availableChallenges = new List<(Challenge challenge, State status)>();
        var activeChallenges = playerChallengeState[steamId];
        var oldChallenges = playerChallengeStats[steamId];
        foreach (var challenge in _challenges)
        {
            var status = State.NotStarted;
            if (activeChallenges.TryGetValue(challenge.id, out var state))
            {
                status = state.Stages.Select(stage => stage.CurrentState())
                    .FirstOrDefault(s => s != State.StageComplete, State.StageComplete);
            }
            else if (oldChallenges.TryGetValue(challenge.id, out var stats))
            {
                if (stats.completeCount > 0)
                {
                    // If we have completed this previously and it can't be repeated, ignore it here.
                    if (!challenge.canRepeat && hideCompleted) continue;
                    
                    status = State.StageComplete;
                }
                else if (stats.attempts > 0)
                {
                    status = challenge.canRepeat ? State.NotStarted : State.Failed;
                }
                else
                {
                    status = State.NotStarted;
                }
            }

            availableChallenges.Add((challenge, status));
        }
        return availableChallenges.AsReadOnly();
    }

    public static void ToggleChallenge(ulong steamId, int index)
    {
        if (index < _challenges.Count)
        {
            var challenge = _challenges[index];
            
            // Stop users from adding challenges twice
            var activeChallenges = playerChallengeState[steamId];
            if (activeChallenges.TryGetValue(challenge.id, out var activeState))
            {
                if (!activeState.CurrentState().IsFinished())
                {
                    Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"{challenge.id} already active: {steamId}");
                    // Mark this as failed as the player is rejecting it
                    activeState.Fail();
                    
                    // Update the challenge as it will be marked as failed
                    UpdateChallenge(challenge.id, steamId);
                    return;
                }
                // if this challenge is finished, then it will be listed in the stats section and we handle it there for other cases
            }
            
            // Stop users from restarting failed/completed challenges that are not repeatable
            var oldChallenges = playerChallengeStats[steamId];
            if (oldChallenges.ContainsKey(challenge.id) && !challenge.canRepeat)
            {
                Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"{challenge.id} not repeatable: {steamId}");
                Output.SendMessage(steamId, L10N.Get(L10N.TemplateKey.ChallengeNotRepeatable));
                return;
            }
            
            var stages = challenge.objectives.Select((stage, stageIndex) =>
            {
                return new Stage(stageIndex,
                    stage.Select<Objective, IObjectiveTracker>(objective =>
                    {
                        if (objective.killCount > 0)
                        {
                            return new KillObjectiveTracker(challenge.id, steamId, objective.killCount);
                        }

                        return new InvalidObjective();
                    }).ToList());
            });
            var activeChallenge = new ChallengeState()
            {
                ChallengeId = challenge.id,
                Stages = stages.ToList()
            };
            // Add the state to the known player challenges
            activeChallenges[challenge.id] = activeChallenge;
            
            // Update the stats as well
            var stats = oldChallenges[challenge.id];
            stats.attempts++;
            oldChallenges[challenge.id] = stats;
            
            // Update the challenge as started
            UpdateChallenge(challenge.id, steamId);
        }
        else
        {
            Output.SendMessage(steamId, L10N.Get(L10N.TemplateKey.ChallengeNotFound));
        }
    }
    
    public static void ToggleChallenge(ulong steamId, string challengeId)
    {
        for (var i = 0; i < _challenges.Count; ++i)
        {
            if (_challenges[i].id == challengeId)
            {
                ToggleChallenge(steamId, i);
                return;
            }            
        }
    }

    public static void UpdateChallenge(string challengeId, ulong steamId)
    {
        var playerChallenges = playerChallengeState[steamId];

        // Handle the case where there are no stages to this challenge (just return)
        if (!playerChallenges.TryGetValue(challengeId, out var challengeUpdated) || challengeUpdated.Stages.Count == 0) return;
        
        // Find the current active stage
        var activeStageIndex = challengeUpdated.UpdateStage(steamId, out var currentState);
        
        Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"Challenge updated: {steamId}-{activeStageIndex}-{currentState}");
        
        if (activeStageIndex == challengeUpdated.Stages.Count)
        {
            // The last stage has been completed (and thus the whole challenge)
            LogChallengeUpdate(steamId, challengeUpdated.Stages[activeStageIndex - 1], State.ChallengeComplete);
            var challengeStats = playerChallengeStats[steamId];
            var stats = challengeStats[challengeId];
            stats.completeCount++;
            stats.lastCompleted = DateTime.Now;
            if (stats.firstCompleted == DateTime.MinValue) stats.firstCompleted = stats.lastCompleted;
            challengeStats[challengeId] = stats;
        }
        else
        {
            // Update the current stage
            LogChallengeUpdate(steamId, challengeUpdated.Stages[activeStageIndex], currentState);
        }
        
        // Send UI update now
        ClientActionHandler.SendChallengeUpdate(steamId, challengeId, challengeUpdated);

        // If this challenge is finished, mark it to be removed from the active challenges
        if (currentState.IsFinished())
        {
            ChallengesToRemove.Add(new ChallengeEnded() {challengeId = challengeId, steamId = steamId, endStatus = currentState, removeTime = DateTime.Now.AddSeconds(5)});
        }
    }

    private static void LogChallengeUpdate(ulong steamId, Stage stage, State status)
    {
        // Only log this if the user is logging
        if (!IsPlayerLoggingChallenges(steamId)) return;
        
        // Should not get into here with a status of NotStarted. Everything should be in progress, complete or failed.
        if (status == State.NotStarted) return;

        var averageProgress = stage.CurrentProgress() * 100f;

        L10N.LocalisableString message;
        switch (status)
        {
            case State.InProgress:
                message = L10N.Get(L10N.TemplateKey.ChallengeProgress).AddField("{progress}", $"{averageProgress:F2}");
                break;
            case State.Failed:
                message = L10N.Get(L10N.TemplateKey.ChallengeFailed);
                break;
            case State.StageComplete:
                message = L10N.Get(L10N.TemplateKey.ChallengeStageComplete);
                break;
            case State.ChallengeComplete:
                message = L10N.Get(L10N.TemplateKey.ChallengeComplete);
                break;
            default:
                // There is no supported state to report here
                return;
        }
        Output.SendMessage(steamId, message);
    }
    
    public static Challenge CreateChallenge(List<List<Objective>> stages, string label, bool repeatable)
    {
        return new Challenge()
        {
            id = Guid.NewGuid().ToString(),
            objectives = stages,
            label = label,
            canRepeat = repeatable
        };
    }

    public static List<Objective> CreateKillStage(int killCount)
    {
        return new List<Objective>
        {
            new()
            {
                killCount = killCount,
                limit = TimeSpan.FromMinutes(5)
            }
        };
    }

    private static void UpdateRemovedChallenges()
    {
        var now = DateTime.Now;
        while (ChallengesToRemove.Count > 0)
        {
            var challenge = ChallengesToRemove[0];
            if (now >= challenge.removeTime)
            {
                // Check to see that it hasn't been restarted
                var activeChallenges = playerChallengeState[challenge.steamId];
                if (activeChallenges.TryGetValue(challenge.challengeId, out var state))
                {
                    var currentState = state.CurrentState();
                    if (!currentState.IsFinished())
                    {
                        // This is not finished yet (likely restarted). Remove it from the remove list
                        ChallengesToRemove.RemoveAt(0);
                        // move along to the next challenge to remove
                        continue;
                    }
                    
                    // send remove to UI
                    ClientActionHandler.SendChallengeUpdate(challenge.steamId, challenge.challengeId, state, true);
                }
                ChallengesToRemove.RemoveAt(0);
                Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"Removing: {challenge.steamId} {challenge.challengeId}");
            }
            else
            {
                // Not ready to remove any more (challenges are ordered by time)
                break;
            }
        }
    }

    private static List<Challenge> testChallenges = new List<Challenge>
    {
        CreateChallenge(new ()
            {
                CreateKillStage(2),
                CreateKillStage(5)
            },
            "Kill stuff",
            true
        ),
    };

    public static Challenge GetChallenge(string challengeId)
    {
        return _challenges.Find(challenge => challenge.id == challengeId);
    }
}