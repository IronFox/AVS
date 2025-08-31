using AVS.Log;
using AVS.SaveLoad;
using AVS.Util;
using HarmonyLib;
using System.Collections.Generic;

// PURPOSE: allow custom save file sprites to be displayed
// VALUE: High.

namespace AVS.Patches;

/// <summary>
/// Patcher to load the ping tech types from a save file.
/// </summary>
[HarmonyPatch(typeof(SaveLoadManager))]
public class SaveLoadManagerPatcher
{
    internal static string GetSaveFileSpritesFileName(RootModController rmc) => rmc.ModName + "SaveFileSprites";

    private static readonly Dictionary<string, List<string>> hasTechTypeGameInfo = new();


    internal static bool GetTechTypeGameInfo(string slotName, out IReadOnlyList<string> hasTechTypes)
    {
        if (hasTechTypeGameInfo.TryGetValue(slotName, out var t))
        {
            hasTechTypes = t;
            return true;
        }
        else
        {
            hasTechTypes = [];
            return false;

        }
    }

    /// <summary>
    /// This patch collects hasTechTypeGameInfo, in order to have save file sprites displayed on the save cards
    /// Postfix method for the SaveLoadManager.RegisterSaveGame method, enabling the collection and management
    /// of custom save file sprites associated with save slots.
    /// </summary>
    /// <param name="slotName">The name of the save slot being registered.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(SaveLoadManager.RegisterSaveGame))]
    public static void SaveLoadManagerRegisterSaveGamePostfix(string slotName)
    {
        using var log = SmartLog.ForAVS(RootModController.AnyInstance, parameters: [slotName], tags: [slotName]);
        try
        {
            //hasTechTypeGameInfo.Clear();
            foreach (var rmc in RootModController.AllInstances)
            {
                SaveFiles.OfSlot(slotName)
                    .ReadReflected<List<string>>(GetSaveFileSpritesFileName(rmc), out var hasTechTypes, rmc);
                if (hasTechTypes.IsNotNull())
                {
                    if (!hasTechTypeGameInfo.TryGetValue(slotName, out var list))
                    {
                        list = [];
                        hasTechTypeGameInfo[slotName] = list;
                    }
                    list.AddRange(hasTechTypes);

                    log.Debug(
                        $"SaveLoadManager.RegisterSaveGamePostfix: Registered {hasTechTypes.Count} new TechTypes for save slot '{slotName}' from mod '{rmc.ModName}'");
                }
            }
        }
        catch (System.Exception e)
        {
            log.Error("SaveLoadManager.RegisterSaveGamePostfix: Could not read json file!", e);
        }
    }
}