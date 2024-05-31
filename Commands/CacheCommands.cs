using VampireCommandFramework;
using XPRising.Systems;
using XPRising.Utils;

namespace XPRising.Commands
{
    public static class CacheCommands {
        [Command("db save", usage: "[saveBackup]", description: "Force the plugin to write XPRising DB to file. Use saveBackup to additionally save to the backup directory.", adminOnly: false)]
        public static void SaveCommand(ChatCommandContext ctx, bool saveBackup = false){
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.DBSave));
            if (AutoSaveSystem.SaveDatabase(true, saveBackup)) Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.DBSaveComplete));
            else Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.DBSaveError));
        }
        
        [Command("db load", usage: "[loadBackup]", description: "Force the plugin to load XPRising DB from file. Use loadBackup to load from the backup directory instead of the main directory.", adminOnly: false)]
        public static void LoadCommand(ChatCommandContext ctx, bool loadBackup = false){
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.DBLoad));
            if (AutoSaveSystem.LoadDatabase(loadBackup)) Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.DBLoadComplete));
            else Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.DBLoadError));
        }
        
        [Command("db wipe", description: "Force the plugin to wipe and re-initialise the database.", adminOnly: false)]
        public static void WipeCommand(ChatCommandContext ctx){
            Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.DBWipe));
            if (AutoSaveSystem.WipeDatabase()) Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.DBWipeComplete));
            else Output.ChatReply(ctx, L10N.Get(L10N.TemplateKey.DBWipeError));
        }
    }
}
