using BepInEx.Logging;
using HarmonyLib;
using ProjectM;
using XPRising.Utils;

namespace XPRising.Hooks
{
    [HarmonyPatch(typeof(TriggerPersistenceSaveSystem), nameof(TriggerPersistenceSaveSystem.TriggerSave))]
    public class TriggerPersistenceSaveSystem_Patch
    {
        public static void Postfix()
        {
            Plugin.Log(Plugin.LogSystem.Core, LogLevel.Info, "Autosaving...", true);
            AutoSaveSystem.SaveDatabase(false, false);
        }
    }
}
