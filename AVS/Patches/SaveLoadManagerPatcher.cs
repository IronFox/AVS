using AVS.Log;
using AVS.SaveLoad;
using HarmonyLib;
using System.Collections.Generic;

// PURPOSE: allow custom save file sprites to be displayed
// VALUE: High.

namespace AVS.Patches
{
    // See also: MainMenuLoadPanelPatcher
    [HarmonyPatch(typeof(SaveLoadManager))]
    public class SaveLoadManagerPatcher
    {
        public const string SaveFileSpritesFileName = "SaveFileSprites";
        public static Dictionary<string, List<string>> hasTechTypeGameInfo = new Dictionary<string, List<string>>();

        // This patch collects hasTechTypeGameInfo, in order to have save file sprites displayed on the save cards
        [HarmonyPostfix]
        [HarmonyPatch(nameof(SaveLoadManager.RegisterSaveGame))]
        public static void SaveLoadManagerRegisterSaveGamePostfix(string slotName)
        {
            try
            {
                SaveFiles.OfSlot(slotName).ReadReflected<List<string>>(SaveFileSpritesFileName, out var hasTechTypes, LogWriter.Default);
                if (hasTechTypes != null)
                {
                    if (hasTechTypeGameInfo.ContainsKey(slotName))
                    {
                        hasTechTypeGameInfo[slotName] = hasTechTypes;
                    }
                    else
                    {
                        hasTechTypeGameInfo.Add(slotName, hasTechTypes);
                    }
                    LogWriter.Default.Debug(
                        $"SaveLoadManager.RegisterSaveGamePostfix: Registered {hasTechTypes.Count} TechTypes for save slot '{slotName}'");
                }
            }
            catch (System.Exception e)
            {
                Logger.LogException("SaveLoadManager.RegisterSaveGamePostfix: Could not read json file!", e);
            }
        }
    }
}
