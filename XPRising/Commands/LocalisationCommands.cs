using VampireCommandFramework;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands;

public static class LocalisationCommands
{
    [Command(name: "l10n", adminOnly: false, usage: "", description: "List available localisations")]
    public static void Localisations(ChatCommandContext ctx)
    {
        Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.LocalisationsAvailable).AddField("{languages}", string.Join(",", L10N.Languages)));
    }
    
    [Command(name: "l10n set", shortHand: "l10n s", adminOnly: false, usage: "<language>", description: "Set your localisation language")]
    public static void SetPlayerLocalisation(ChatCommandContext ctx, string language)
    {
        var playerPreferences = Database.PlayerPreferences[ctx.User.PlatformId];
        playerPreferences.Language = language;
        Database.PlayerPreferences[ctx.User.PlatformId] = playerPreferences;
        
        Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.LocalisationSet).AddField("{language}", language));
    }
}
