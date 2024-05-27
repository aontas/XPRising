using VampireCommandFramework;
using XPRising.Utils;

namespace XPRising.Commands
{
    public static class CacheCommands {
        [Command("db save", usage: "[saveBackup]", description: "Force the plugin to write XPRising DB to file. Use saveBackup to additionally save to the backup directory.", adminOnly: false)]
        public static void SaveCommand(ChatCommandContext ctx, bool saveBackup = false){
            ctx.Reply($"Saving data....");
            if (AutoSaveSystem.SaveDatabase(true, saveBackup)) ctx.Reply($"Data save complete.");
            else ctx.Reply($"Error saving data. See server BepInEx log for details.");
        }
        
        [Command("db load", usage: "[loadBackup]", description: "Force the plugin to load XPRising DB from file. Use loadBackup to load from the backup directory instead of the main directory.", adminOnly: false)]
        public static void LoadCommand(ChatCommandContext ctx, bool loadBackup = false){
            ctx.Reply($"Loading data....");
            if (AutoSaveSystem.LoadDatabase(loadBackup)) ctx.Reply($"Data load complete.");
            else ctx.Reply("Error loading data. Data that failed to load was not overwritten in currently loaded data. See server BepInEx log for details.");
        }
        
        [Command("db wipe", description: "Force the plugin to wipe and re-initialise the database.", adminOnly: false)]
        public static void WipeCommand(ChatCommandContext ctx){
            ctx.Reply($"Wiping data....");
            if (AutoSaveSystem.WipeDatabase()) ctx.Reply($"Data load complete.");
            else ctx.Reply("Error wiping data. See server BepInEx log for details.");
        }
    }
}
