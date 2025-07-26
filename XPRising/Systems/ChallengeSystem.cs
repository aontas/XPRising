using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using BepInEx.Logging;
using XPRising.Models;
using XPRising.Models.Challenges;
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
         * Challenge options without variable support yet:
         * - spell school use
         * - player blood type
         * - zone (eg, mortium vs farbane woods)
         * - location
         * - survive
         * - time of day (e.g. kills at night vs kills during day)
         * - level difference range
         */
        // Kills required
        public int killCount;
        // Damage done
        public float damageCount;
        // List of units accepted for counting as kills/damage
        public List<Units> unitTypes;
        // List of factions accepted for counting as kills/damage
        public List<Faction> factions;
        // List of blood types accepted for counting as kills/damage
        public List<BloodType> unitBloodType;
        // Minimum blood level accepted for kills/damage
        public float bloodLevel;
        // List of weapon/spell types accepted for kills/damage
        // - would be good to extend masteries to include individual spell schools for this type
        public List<GlobalMasterySystem.MasteryType> masteryTypes;
        // Required time limit
        // - (positive) requires kills/damage to be completed in time
        // - (negative) records score generated from kills/damage within time limit 
        public TimeSpan limit;
        // Challenge checked for completion at given time
        // - used for dynamically created challenges (i.e. players must be at location at given time before continuing to next stage)
        public DateTime time;
        // Used for multiplayer. Player must have placement > x to continue in challenge (i.e. knockout style)
        public int placement;
    }

    public struct Challenge
    {
        public string ID;
        public string Label;
        public List<List<Objective>> Objectives;
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        public bool CanRepeat;
        // Reward?
    }

    public class ChallengeConfig
    {
        public List<Challenge> Challenges;
        public List<Challenge> ChallengeTemplates;
    }

    public struct ChallengeStats
    {
        public DateTime FirstCompleted;
        public DateTime LastCompleted;
        public int Attempts;
        public int CompleteCount;
        public TimeSpan FastestTime;
        public int Score;
    }

    public static ChallengeConfig ChallengeDatabase;
    public static LazyDictionary<ulong, LazyDictionary<string, ChallengeStats>> PlayerChallengeStats = new();

    private static readonly LazyDictionary<ulong, LazyDictionary<string, ChallengeState>> PlayerActiveChallenges = new();

    private struct ChallengeEnded
    {
        public string challengeId;
        public ulong steamId;
        public State endStatus;
        public DateTime removeTime;
    }
    
    // A list of challenges that have failed/completed so that we can remove them from the active list in the UI
    private static readonly List<ChallengeEnded> ChallengesToRemove = new();
    private static readonly FrameTimer RemoveTimer = new FrameTimer();
    
    public static bool IsPlayerLoggingChallenges(ulong steamId)
    {
        return Database.PlayerPreferences[steamId].LoggingChallenges;
    }

    public static void Initialise()
    {
        if (Plugin.ChallengeSystemActive)
        {
            RemoveTimer.Initialise(UpdateRemovedChallenges, TimeSpan.FromMilliseconds(500), -1);
            RemoveTimer.Start();
        }
    }

    public static ReadOnlyCollection<(Challenge challenge, State status)> ListChallenges(ulong steamId, bool hideCompleted = true)
    {
        var availableChallenges = new List<(Challenge challenge, State status)>();
        var activeChallenges = PlayerActiveChallenges[steamId];
        var oldChallenges = PlayerChallengeStats[steamId];
        foreach (var challenge in ChallengeDatabase.Challenges)
        {
            var status = State.NotStarted;
            if (activeChallenges.TryGetValue(challenge.ID, out var state))
            {
                status = state.Stages.Select(stage => stage.CurrentState())
                    .FirstOrDefault(s => s != State.Complete, State.Complete);
            }
            else if (oldChallenges.TryGetValue(challenge.ID, out var stats))
            {
                if (stats.CompleteCount > 0)
                {
                    // If we have completed this previously and it can't be repeated, ignore it here.
                    if (!challenge.CanRepeat && hideCompleted) continue;
                    
                    status = State.Complete;
                }
                else if (stats.Attempts > 0)
                {
                    status = challenge.CanRepeat ? State.NotStarted : State.Failed;
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
        if (index < ChallengeDatabase.Challenges.Count)
        {
            var challenge = ChallengeDatabase.Challenges[index];
            
            // Stop users from adding challenges twice
            var activeChallenges = PlayerActiveChallenges[steamId];
            if (activeChallenges.TryGetValue(challenge.ID, out var activeState))
            {
                if (!activeState.CurrentState().IsFinished())
                {
                    Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"{challenge.ID} already active: {steamId}");
                    // Mark this as failed as the player is rejecting it
                    activeState.Fail();
                    
                    // Update the challenge as it will be marked as failed
                    UpdateChallenge(challenge.ID, steamId);
                    return;
                }
                // if this challenge is finished, then it will be listed in the stats section and we handle it there for other cases
            }
            
            // Stop users from restarting failed/completed challenges that are not repeatable
            var oldChallenges = PlayerChallengeStats[steamId];
            if (oldChallenges.ContainsKey(challenge.ID) && !challenge.CanRepeat)
            {
                Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"{challenge.ID} not repeatable: {steamId}");
                Output.SendMessage(steamId, L10N.Get(L10N.TemplateKey.ChallengeNotRepeatable));
                return;
            }
            
            var stages = challenge.Objectives.Select((stage, stageIndex) =>
            {
                var objectives = new List<IObjectiveTracker>();
                var limit = TimeSpan.Zero;
                foreach (var objective in stage)
                {
                    if (objective.killCount != 0)
                    {
                        objectives.Add(new KillObjectiveTracker(challenge.ID, steamId, objectives.Count, stageIndex, objective.killCount, objective.factions, objective.unitBloodType));
                    }

                    if (objective.limit.TotalSeconds != 0)
                    {
                        // Update the limit if it is zero (not yet set) or if the new limit is smaller.
                        limit = limit == TimeSpan.Zero || limit > objective.limit ? objective.limit : limit;
                    }
                }

                if (objectives.Count == 0)
                {
                    objectives.Add(new InvalidObjective(0, stageIndex));
                }
                // Only add a limit if there is an actual objective
                else if (limit.TotalSeconds != 0)
                {
                    objectives.Add(new TimeLimitTracker(challenge.ID, steamId, objectives.Count, stageIndex, limit));
                }
                return new Stage(stageIndex, objectives);
            });
            var activeChallenge = new ChallengeState()
            {
                ChallengeId = challenge.ID,
                Stages = stages.ToList()
            };
            // Add the state to the known player challenges
            activeChallenges[challenge.ID] = activeChallenge;
            
            // Update the stats as well
            var stats = oldChallenges[challenge.ID];
            stats.Attempts++;
            oldChallenges[challenge.ID] = stats;
            
            // Update the challenge as started
            UpdateChallenge(challenge.ID, steamId);
        }
        else
        {
            Output.SendMessage(steamId, L10N.Get(L10N.TemplateKey.ChallengeNotFound));
        }
    }
    
    public static void ToggleChallenge(ulong steamId, string challengeId)
    {
        for (var i = 0; i < ChallengeDatabase.Challenges.Count; ++i)
        {
            if (ChallengeDatabase.Challenges[i].ID == challengeId)
            {
                ToggleChallenge(steamId, i);
                return;
            }            
        }
    }

    public static ReadOnlyCollection<(ulong, ChallengeStats)> ListChallengeStats(int index, int top, out Challenge challenge)
    {
        var challengeStats = new List<(ulong, ChallengeStats)>();
        challenge = new Challenge();

        if (index >= ChallengeDatabase.Challenges.Count) return challengeStats.AsReadOnly();
        
        challenge = ChallengeDatabase.Challenges[index];
        foreach (var (playerStatId, playerStats) in PlayerChallengeStats)
        {
            // Ignore this entry if the player has not attempted it or if they have not completed it
            if (!playerStats.TryGetValue(challenge.ID, out var stats) || stats.CompleteCount == 0) continue;

            challengeStats.Add((playerStatId, stats));
        }
        // OrderBy time (ASC) ThenBy score (DESC)
        return challengeStats.OrderBy(x => x.Item2.FastestTime).ThenByDescending(x => x.Item2.Score).Take(top).ToList().AsReadOnly();
    }

    public static void UpdateChallenge(string challengeId, ulong steamId)
    {
        var playerChallenges = PlayerActiveChallenges[steamId];

        // Handle the case where there are no stages to this challenge (just return)
        if (!playerChallenges.TryGetValue(challengeId, out var challengeUpdated) || challengeUpdated.Stages.Count == 0) return;
        
        // Find the current active stage
        var activeStageIndex = challengeUpdated.UpdateStage(steamId, out var currentState);
        
        Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Info, $"Challenge updated: {steamId}-{activeStageIndex}-{currentState}");
        
        // The active stage has progressed passed the end (so all the stages have been completed)
        if (activeStageIndex == challengeUpdated.Stages.Count)
        {
            // Send an update to the UI about completing the previous stage
            LogChallengeUpdate(steamId, challengeUpdated.Stages[activeStageIndex - 1], State.ChallengeComplete);
            var challengeStats = PlayerChallengeStats[steamId];
            var stats = challengeStats[challengeId];
            stats.CompleteCount++;
            stats.LastCompleted = DateTime.Now;

            challengeUpdated.CalculateScore(out var timeTaken, out var score);
            if (stats.FirstCompleted == DateTime.MinValue)
            {
                stats.FirstCompleted = stats.LastCompleted;
                stats.FastestTime = timeTaken;
                stats.Score = score;
            }
            else if (stats.FastestTime > timeTaken)
            {
                stats.FastestTime = timeTaken;
                stats.Score = score;
            }
            else if (stats.FastestTime == timeTaken && stats.Score < score)
            {
                stats.Score = score;
            }
            
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

    public static Challenge GetChallenge(string challengeId)
    {
        return ChallengeDatabase.Challenges.Find(challenge => challenge.ID == challengeId);
    }

    public static void ValidateChallenges()
    {
        var knownIDs = new HashSet<string>();
        try
        {
            ChallengeDatabase.Challenges = ChallengeDatabase.Challenges.Select(challenge =>
            {
                // Make sure IDs are set and are unique
                if (challenge.ID == "" || knownIDs.Contains(challenge.ID))
                {
                    challenge.ID = Guid.NewGuid().ToString();
                }

                knownIDs.Add(challenge.ID);

                return challenge;
            }).ToList();
            
            // Remove stats for challenges that no longer exist
            // Note that removing challenges is in the try so that if there is an issue parsing the challenges, the history is not removed
            foreach (var (steamId, stats) in PlayerChallengeStats)
            {
                foreach (var challengeId in stats.Keys.Where(challengeId => !knownIDs.Contains(challengeId)))
                {
                    stats.Remove(challengeId);
                }
            }
        }
        catch
        {
            // Challenge validation failed, disabling the system
            Plugin.ChallengeSystemActive = false;
            Plugin.Log(Plugin.LogSystem.Challenge, LogLevel.Error, "Disabling Challenge system. Configuration validation failed. Check challenge JSON configuration.", true);
        }
    }

    private static void LogChallengeUpdate(ulong steamId, Stage stage, State status)
    {
        // Only log this if the user is logging
        if (!IsPlayerLoggingChallenges(steamId)) return;
        
        // Should not get into here with a status of NotStarted. Everything should be in progress, complete or failed.
        if (status == State.NotStarted) return;

        L10N.LocalisableString message;
        switch (status)
        {
            case State.InProgress:
                var averageProgress = stage.CurrentProgress();
                string progress;
                if (averageProgress >= 0)
                {
                    progress = $"{averageProgress:P0}";
                }
                else
                {
                    // TODO also get saving/loading on disk
                    stage.CalculateScore(out _, out var score);
                    progress = $"{score:F0}";
                }
                message = L10N.Get(L10N.TemplateKey.ChallengeProgress)
                    .AddField("{stage}", $"{stage.Index + 1:D}")
                    .AddField("{progress}", progress);
                break;
            case State.Failed:
                message = L10N.Get(L10N.TemplateKey.ChallengeFailed);
                break;
            case State.Complete:
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
            ID = Guid.NewGuid().ToString(),
            Objectives = stages,
            Label = label,
            CanRepeat = repeatable
        };
    }

    public static Objective CreateKillObjective(int killCount, int minutes, List<Faction> factions = null, List<BloodType> bloodTypes = null)
    {
        return new Objective()
        {
            killCount = killCount,
            limit = TimeSpan.FromMinutes(minutes),
            factions = factions,
            unitBloodType = bloodTypes,
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
                var activeChallenges = PlayerActiveChallenges[challenge.steamId];
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

    public static ChallengeConfig DefaultBasicChallenges()
    {
        return new ChallengeConfig()
        {
            Challenges = new List<Challenge>
            {
                CreateChallenge(new()
                    {
                        new List<Objective>() {
                            CreateKillObjective(10, 0, new List<Faction>() { Faction.Bandits }),
                            CreateKillObjective(10, 0, new List<Faction>() { Faction.Undead }),
                            CreateKillObjective(10, 0, new List<Faction>() { Faction.Wolves }) },
                        new List<Objective>() { CreateKillObjective(1, 0, bloodTypes: new List<BloodType>()
                            {
                                BloodType.VBlood
                            }) }
                    },
                    "Farbane menace",
                    true
                ),
                CreateChallenge(new()
                    {
                        new List<Objective>() { CreateKillObjective(-1, -10, new List<Faction>() { Faction.Bandits, Faction.Wolves }) },
                    },
                    "Kill bandits in 10m",
                    true
                ),
            },
            ChallengeTemplates = new List<Challenge>()
        };
    }
}