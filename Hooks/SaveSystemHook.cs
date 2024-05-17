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
            AutoSaveSystem.SaveDatabase(false, false);
        }
    }
}
