using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using BepInEx.Logging;
using XPRising.Utils;
using static XPRising.Models.DefaultLocalisations;

namespace XPRising.Systems;

public static class L10N
{
    private const string DefaultUnknown = "No default display string for {key}";
    private const string ExampleLocalisationFile = "example_localisation_template.json";
    public const string DefaultLanguage = "en_AU";
    public static string DefaultUserLanguage = DefaultLanguage;

    private static HashSet<string> _languages = new();
    private static Dictionary<TemplateKey, Dictionary<string, string>> templates = new();
    public static Dictionary<ulong, string> UserLanguage = new();
    
    public static string LanguagesPath => Path.Combine(AutoSaveSystem.ConfigPath, "Languages");
    public static ReadOnlyCollection<string> Languages => _languages.OrderBy(x => x).ToList().AsReadOnly();
    
    private static LocalisableString NoLocalisation
    {
        get
        {
            if (templates.TryGetValue(TemplateKey.GeneralUnknown, out var localisations))
            {
                return new LocalisableString(localisations);
            }
            return new LocalisableString(DefaultUnknown);
        }
    }

    public enum TemplateKey
    {
        AllianceAddSelfError,
        AllianceAlreadyInvited,
        AllianceCurrentInvites,
        AllianceGroupEmpty,
        AllianceGroupIgnore,
        AllianceGroupInfoNone,
        AllianceGroupInvited,
        AllianceGroupLeft,
        AllianceGroupListen,
        AllianceGroupLoggedOut,
        AllianceGroupMembers,
        AllianceGroupNull,
        AllianceGroupOtherJoined,
        AllianceGroupOtherLeft,
        AllianceGroupWipe,
        AllianceIgnoringInvites,
        AllianceInOtherGroup,
        AllianceInvite404,
        AllianceInviteAccepted,
        AllianceInviteGroup404,
        AllianceInviteMaxPlayers,
        AllianceInviteRejected,
        AllianceInviteSent,
        AllianceInvitesNone,
        AllianceInYourGroup,
        AllianceMaxGroupSize,
        AllianceNoNearPlayers,
        AlliancePreferences,
        BloodAdjusted,
        BloodlineDecay,
        BloodlineHeader,
        BloodlineMasteryGain,
        BloodlineMercilessErrorBlood,
        BloodlineMercilessErrorWeak,
        BloodlineMercilessUnmatchedBlood,
        BloodNoValue,
        BloodReset,
        BloodSet,
        BloodType404,
        BloodUnknown,
        DBLoad,
        DBLoadComplete,
        DBLoadError,
        DBSave,
        DBSaveComplete,
        DBSaveError,
        DBWipe,
        DBWipeComplete,
        DBWipeError,
        GeneralPlayerNotFound,
        GeneralUnknown,
        LocalisationsAvailable,
        LocalisationSet,
        MasteryAdjusted,
        MasteryDecay,
        MasteryGain,
        MasteryFull,
        MasteryHeader,
        MasterySpellHeader,
        MasterySpellDescription,
        MasteryEffectiveness,
        MasteryIncrease,
        MasteryIncreasedStat,
        MasteryAddedStat,
        MasteryNoValue,
        MasteryReset,
        MasterySet,
        MasteryType404,
        PermissionCommandSet,
        PermissionCommandUnknown,
        PermissionModifyHigherError,
        PermissionModifySelfError,
        PermissionNoCommands,
        PermissionNoUsers,
        PermissionPlayerSet,
        PlayerInfoName,
        PlayerInfoSteamID,
        PlayerInfoLatency,
        PlayerInfoAdmin,
        PlayerInfoPosition,
        PlayerInfoOffline,
        PlayerInfoBuffs,
        PlayerInfoNoBuffs,
        PowerPointsAvailable,
        PowerPointsNotEnough,
        PowerPointsReset,
        PowerPointsSpendError,
        PowerPointsSpent,
        SystemEffectivenessDisabled,
        SystemLogDisabled,
        SystemLogEnabled,
        SystemNotEnabled,
        WantedFactionHeatStatus,
        WantedFactionUnsupported,
        WantedHeatDataEmpty,
        WantedHeatDecrease,
        WantedHeatIncrease,
        WantedLevelSet,
        WantedLevelsNone,
        WantedTriggerAmbush,
        WantedMinionRemoveError,
        WantedMinionRemoveSuccess,
        XpAdminBump,
        XpBump,
        XpBumpError,
        XpGain,
        XpLevel,
        XpLevelUp,
        XpLost,
        XpSet
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

        var noLocalisation = NoLocalisation.AddField("{key}", Enum.GetName(key));
        Plugin.Log(Plugin.LogSystem.Core, LogLevel.Error, noLocalisation.Build(DefaultLanguage));
        return noLocalisation;
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
        var defaultLocalisations = AllDefaultLocalisations;
        foreach (var key in Enum.GetValues<TemplateKey>())
        {
            foreach (var languageData in defaultLocalisations)
            {
                if (languageData.localisations.TryGetValue(key, out var localisation))
                {
                    AddLocalisation(key, languageData.language, localisation);
                }
            }
        }
    }

    public struct LanguageData
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
        var foundExampleFile = false;

        foreach(var file in files)
        {
            foundExampleFile = foundExampleFile || file.Name == ExampleLocalisationFile;
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
        
        // Regenerate example file if it is removed
        if (!foundExampleFile)
        {
            try
            {
                var outputFile = Path.Combine(LanguagesPath, ExampleLocalisationFile);
                
                // Add any unset values to the default, in-case there are missing ones that we haven't added yet
                foreach (var templateKey in Enum.GetValues<TemplateKey>())
                {
                    if (!LocalisationAU.localisations.ContainsKey(templateKey))
                    {
                        var noLocalisation = NoLocalisation.AddField("{key}", Enum.GetName(templateKey));
                        LocalisationAU.localisations.Add(templateKey, noLocalisation.Build(LocalisationAU.language));
                    }
                }

                File.WriteAllText(outputFile, JsonSerializer.Serialize(LocalisationAU, AutoSaveSystem.PrettyJsonOptions));

                Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, $"Language file saved: {ExampleLocalisationFile}");
            }
            catch (Exception e)
            {
                Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info,
                    $"Failed saving language file: ${ExampleLocalisationFile}: {e.Message}");
            }
        }
    }
}