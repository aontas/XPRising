using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using BepInEx.Logging;
using XPRising.Utils;

namespace XPRising.Systems;

public static class LocalisationSystem
{
    private const string ExampleLocalisationFile = "example_localisation_template.json";
    public const string DefaultLanguage = "en_AU";
    public static string DefaultUserLanguage = DefaultLanguage;

    private static HashSet<string> _languages = new();
    private static Dictionary<TemplateKey, Dictionary<string, string>> templates = new();
    public static Dictionary<ulong, string> UserLanguage = new();
    
    public static string LanguagesPath => Path.Combine(AutoSaveSystem.ConfigPath, "Languages");
    public static ReadOnlyCollection<string> Languages => _languages.OrderBy(x => x).ToList().AsReadOnly();

    public enum TemplateKey
    {
        XpGain,
        XpLost,
        LevelUp,
        XpBump,
        EffectivenessDisabled,
        GroupInvite,
        GroupAccept,
        GroupLeave,
        GroupLoggedOut,
        FactionHeatStatus,
        NoWantedLevels,
        BloodlineMercilessFailBlood,
        BloodlineMercilessUnmatchedBlood,
        BloodlineMercilessFailWeak,
        BloodlineMasteryGain,
        BloodlineDecay,
        WantedHeatDecrease,
        WantedHeatIncrease,
        WantedHeatDataEmpty,
        WeaponMasteryGain,
        WeaponMasteryDecay,
    }

    public static void AddLocalisation(TemplateKey key, string language, string localisation)
    {
        if (!templates.TryGetValue(key, out var localisations))
        {
            templates.Add(key, new Dictionary<string, string>());
            localisations = templates[key];
        }

        localisations[language] = localisation;
        _languages.Add(language);
    }
    
    public static LocalisableString Get(TemplateKey key)
    {
        if (templates.TryGetValue(key, out var template)) return new LocalisableString(template);
        
        Plugin.Log(Plugin.LogSystem.Core, LogLevel.Error, $"No localisation template for {key}");
        return new LocalisableString($"No localisation template for {key}");
    }

    public static string GetUserLanguage(ulong steamID)
    {
        var language = UserLanguage.GetValueOrDefault(steamID, DefaultUserLanguage);

        return language;
    }

    public static void SetUserLanguage(ulong steamID, string language)
    {
        UserLanguage[steamID] = language;
    }

    public class LocalisableString
    {
        private readonly Dictionary<string, string> _localisations;
        private readonly Dictionary<string, string> _replacers = new();

        public LocalisableString(string defaultLocalisation)
        {
            _localisations = new Dictionary<string, string> {{DefaultLanguage, defaultLocalisation}};
        }
        
        public LocalisableString(Dictionary<string, string> localisations)
        {
            _localisations = localisations;
        }

        public LocalisableString AddField(string field, string replacement)
        {
            _replacers[field] = replacement;
            return this;
        }

        public string Build(string language)
        {
            if (!_localisations.TryGetValue(language, out var localisation))
            {
                localisation = _localisations[DefaultLanguage];
            }
            
            return _replacers.Aggregate(localisation, (current, parameter)=> current.Replace(parameter.Key, parameter.Value.ToString()));
        }
    }

    public static void SetDefaultLocalisations()
    {
        foreach (var key in Enum.GetValues<TemplateKey>())
        {
            if (!DefaultLocalisation.TryGetValue(key, out var localisation))
            {
                localisation = $"No default display string for {Enum.GetName(key)}";
            }
            AddLocalisation(key, DefaultLanguage, localisation);
        }
    }

    private static readonly Dictionary<TemplateKey, string> DefaultLocalisation = new()
    {
        {
            TemplateKey.XpGain,
            $"<color={Output.LightYellow}>You gain {{xpGained}} XP by slaying a Lv.{{mobLevel}} enemy.</color> [ XP: <color={Output.White}>{{earned}}</color>/<color={Output.White}>{{needed}}</color> ]"
        },

        {
            TemplateKey.XpLost,
            $"You've been defeated, <color={Output.White}>{{xpLost}}</color> XP is lost. [ XP: <color={Output.White}>{{earned}}</color>/<color={Output.White}>{{needed}}</color> ]"
        },
        {
            TemplateKey.LevelUp,
            $"<color={Output.LightYellow}>Level up! You're now level</color> <color={Output.White}>{{level}}</color><color={Output.LightYellow}>!</color>"
        },
        {
            TemplateKey.EffectivenessDisabled,
            "Effectiveness Subsystem disabled, not resetting {system}."
        },
        {
            TemplateKey.XpBump,
            "You have been bumped to lvl 20 for 5 seconds. Equip an item and then claim the reward."
        },
        {
            TemplateKey.GroupInvite,
            "{user} has joined your group."
        },
        {
            TemplateKey.GroupAccept,
            "{user} has joined your group."
        },
        {
            TemplateKey.GroupLoggedOut,
            "{user} has logged out and left your group."
        },
        {
            TemplateKey.GroupLeave,
            "{user} has left your group."
        },
        {
            // TODO squad message support
            TemplateKey.FactionHeatStatus,
            "<color=#{color}>{squadMessage}</color>"
        },
        {
            TemplateKey.NoWantedLevels,
            "No active wanted levels"
        },
        {
            TemplateKey.BloodlineMercilessFailBlood,
            $"<color={Output.DarkRed}>You have no bloodline to get mastery...</color>"
        },
        {
            TemplateKey.BloodlineMercilessUnmatchedBlood,
            $"<color={Output.DarkRed}>Bloodline is not compatible with yours...</color>"
        },
        {
            TemplateKey.BloodlineMercilessFailWeak,
            $"<color={Output.DarkRed}>Bloodline is too weak to increase mastery...</color>"
        },
        {
            TemplateKey.BloodlineMasteryGain,
            $"<color={Output.DarkYellow}>Bloodline mastery has increased by {{growth}}% [ {{bloodType}}: {{total}}%]</color>"
        },
        {
            TemplateKey.BloodlineDecay,
            "You've been offline for {duration} minute(s). Your bloodline mastery has decayed by {decay}%"
        },
        {
            TemplateKey.WantedHeatDecrease,
            "Wanted level decreased ({factionStatus})"
        },
        {
            TemplateKey.WantedHeatIncrease,
            "Wanted level increased ({factionStatus})"
        },
        {
            TemplateKey.WantedHeatDataEmpty,
            "All heat levels 0"
        },
        {
            TemplateKey.WeaponMasteryGain,
            $"<color={Output.DarkYellow}>Weapon mastery has increased by {{masteryChange}}% [ {{masteryType}}: {{currentMastery}}% ]</color>"
        },
        {
            TemplateKey.WeaponMasteryDecay,
            "You've been offline for {duration} minute(s). Your weapon mastery has decayed by {decay}%"
        },
    };

    private struct LanguageData
    {
        public string language;
        public bool overrideDefaultLanguage;
        public Dictionary<TemplateKey, string> localisations;
    }

    public static void Initialize()
    {
        // Set up default localisations for initial load (and other loads).
        SetDefaultLocalisations();
        
        // Create the languages directory (if needed).
        Directory.CreateDirectory(LanguagesPath);
        
        // Attempt to load any languages in the LanguagesPath folder
        var d = new DirectoryInfo(LanguagesPath);
        var files = d.GetFiles("*.json");

        if (files.Length == 0)
        {
            try
            {
                var outputFile = Path.Combine(LanguagesPath, ExampleLocalisationFile);
                var data = new LanguageData()
                {
                    language = DefaultLanguage,
                    overrideDefaultLanguage = true,
                    localisations = DefaultLocalisation
                };

                File.WriteAllText(outputFile, JsonSerializer.Serialize(data, AutoSaveSystem.PrettyJsonOptions));

                Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info,
                    $"Language file saved: {ExampleLocalisationFile}");
            }
            catch (Exception e)
            {
                Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info,
                    $"Failed saving language file: ${ExampleLocalisationFile}: {e.Message}");
            }
        }

        foreach(var file in files)
        {
            Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, $"Language file saved: ${ExampleLocalisationFile}");
            try {
                var jsonString = File.ReadAllText(file.FullName);
                var data = JsonSerializer.Deserialize<LanguageData>(jsonString, AutoSaveSystem.JsonOptions);
                Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, $"Loaded language file: {file.Name}");

                if (string.IsNullOrEmpty(data.language))
                {
                    Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, $"Missing language property: {file.Name}");
                }

                if (data.overrideDefaultLanguage)
                {
                    DefaultUserLanguage = data.language;
                }

                foreach (var localisation in data.localisations)
                {
                    AddLocalisation(localisation.Key, data.language, localisation.Value);
                }
            } catch (Exception e) {
                Plugin.Log(Plugin.LogSystem.Core, LogLevel.Error, $"Error loading language file: {file.Name}", true);
                Plugin.Log(Plugin.LogSystem.Debug, LogLevel.Error, () => e.ToString());
            }
        }
    }
}