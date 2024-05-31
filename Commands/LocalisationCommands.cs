using VampireCommandFramework;
using XPRising.Systems;

namespace XPRising.Commands;

public static class LocalisationCommands
{
    [Command(name: "l10n", adminOnly: false, usage: "", description: "List available localisations")]
    public static void Localisations(ChatCommandContext ctx)
    {
        ctx.Reply("test");
        ctx.Reply($"Available languages: {string.Join(",", LocalisationSystem.Languages)}");
    }
    
    [Command(name: "l10n set", shortHand: "l10n s", adminOnly: false, usage: "<language>", description: "Set your localisation language")]
    public static void SetPlayerLocalisation(ChatCommandContext ctx, string language)
    {
        ctx.Reply("test2");
        LocalisationSystem.SetUserLanguage(ctx.User.PlatformId, language);
        
        ctx.Reply($"Localisation language set to {language}");
    }
}
