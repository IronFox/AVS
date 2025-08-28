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
    internal static string GetSaveFileSpritesFileName(MainPatcher mp) => mp.ModName + "SaveFileSprites";

    private static readonly Dictionary<string, List<string>> hasTechTypeGameInfo = new();

    /// <summary>
    /// The AVS tech types registered per save slot
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> HasTechTypeGameInfo => (IReadOnlyDictionary<string, IReadOnlyList<string>>)hasTechTypeGameInfo;

    // This patch collects hasTechTypeGameInfo, in order to have save file sprites displayed on the save cards
    /// <summary>
    /// Postfix method for the SaveLoadManager.RegisterSaveGame method, enabling the collection and management
    /// of custom save file sprites associated with save slots.
    /// </summary>
    /// <param name="slotName">The name of the save slot being registered.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(SaveLoadManager.RegisterSaveGame))]
    public static void SaveLoadManagerRegisterSaveGamePostfix(string slotName)
    {
        try
        {
            hasTechTypeGameInfo.Clear();
            foreach (var mp in MainPatcher.AllInstances)
            {
                SaveFiles.OfSlot(slotName)
                    .ReadReflected<List<string>>(GetSaveFileSpritesFileName(mp), out var hasTechTypes, LogWriter.Default);
                if (hasTechTypes.IsNotNull())
                {
                    if (!hasTechTypeGameInfo.TryGetValue(slotName, out var list))
                    {
                        list = [];
                        hasTechTypeGameInfo[slotName] = list;
                    }
                    list.AddRange(hasTechTypes);

                    LogWriter.Default.Debug(
                        $"SaveLoadManager.RegisterSaveGamePostfix: Registered {hasTechTypes.Count} new TechTypes for save slot '{slotName}' from mod '{mp.ModName}'");
                }
            }
        }
        catch (System.Exception e)
        {
            Logger.LogException("SaveLoadManager.RegisterSaveGamePostfix: Could not read json file!", e);
        }
    }
}