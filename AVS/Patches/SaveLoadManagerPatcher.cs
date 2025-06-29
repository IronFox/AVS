using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

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
            string subnauticaPath;
            try
            {
                subnauticaPath = Directory.GetParent(BepInEx.Paths.BepInExRootPath).FullName;
            }
            catch (System.Exception e)
            {
                Logger.LogException("Failed to get parent directory.", e);
                return;
            }
            string savePath;
            try
            {
                savePath = Path.Combine(subnauticaPath, "SNAppData", "SavedGames", slotName, SaveLoad.JsonInterface.SaveFolderName, $"{SaveFileSpritesFileName}.json");
            }
            catch (System.Exception e)
            {
                Logger.LogException("Failed to get parent directory.", e);
                return;
            }
            if (!File.Exists(savePath))
            {
                Logger.Warn($"SaveLoadManager.RegisterSaveGamePostfix failed to find the save game json file: {savePath}");
                return;
            }
            try
            {
                string jsonContent = File.ReadAllText(savePath);
                List<string> hasTechTypes = JsonConvert.DeserializeObject<List<string>>(jsonContent);
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
                }
            }
            catch (System.Exception e)
            {
                Logger.LogException("SaveLoadManager.RegisterSaveGamePostfix: Could not read json file!", e);
            }
        }
    }
}
