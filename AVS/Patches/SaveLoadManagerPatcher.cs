using AVS.Log;
using AVS.SaveLoad;
using HarmonyLib;
using System.Collections.Generic;

// PURPOSE: allow custom save file sprites to be displayed
// VALUE: High.

namespace AVS.Patches
{
    /// <summary>
    /// Patcher to load the ping tech types from a save file.
    /// </summary>
    [HarmonyPatch(typeof(SaveLoadManager))]
    public class SaveLoadManagerPatcher
    {
        internal static string SaveFileSpritesFileName => MainPatcher.Instance.ModName + "SaveFileSprites";

        private static readonly Dictionary<string, IReadOnlyList<string>> hasTechTypeGameInfo = new Dictionary<string, IReadOnlyList<string>>();

        /// <summary>
        /// The AVS tech types registered per save slot
        /// </summary>
        public static IReadOnlyDictionary<string, IReadOnlyList<string>> HasTechTypeGameInfo => hasTechTypeGameInfo;

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
