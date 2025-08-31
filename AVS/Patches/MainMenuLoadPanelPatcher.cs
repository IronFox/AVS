using AVS.Log;
using AVS.Util;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

// PURPOSE: allow custom save file sprites to be displayed
// VALUE: High.

namespace AVS.Patches;

/// <summary>
/// Harmony patch for <see cref="MainMenuLoadPanel"/> to support custom save file sprites.
/// See also: <see cref="SaveLoadManagerPatcher"/>
/// </summary>
[HarmonyPatch(typeof(MainMenuLoadPanel))]
public class MainMenuLoadPanelPatcher
{
    /// <summary>
    /// List of tech types that have associated save file sprites.
    /// </summary>
    public static List<string> HasTechTypes = new();

    /// <summary>
    /// Adds custom save file sprites as child images to the given <see cref="MainMenuLoadButton"/>.
    /// </summary>
    /// <param name="lb">The load button to add sprites to.</param>
    public static void AddLoadButtonSprites(MainMenuLoadButton lb)
    {
        foreach (var ve in AvsVehicleManager.VehicleTypes)
            if (ve.AV.IsNotNull() && ve.AV.Config.SaveFileSprite)
            {
                var techType = ve.TechType.AsString();
                var imageObject = new GameObject(techType);
                imageObject.transform.SetParent(lb.saveIcons.transform, false);
                imageObject.AddComponent<UnityEngine.UI.Image>().sprite = ve.AV.Config.SaveFileSprite;
                imageObject.EnsureComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
                imageObject.SetActive(false);
            }
    }

    /// <summary>
    /// Harmony postfix for <see cref="MainMenuLoadPanel.UpdateLoadButtonState"/>.
    /// Ensures custom save file sprites are displayed and sized correctly.
    /// </summary>
    /// <param name="lb">The load button whose state is being updated.</param>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(MainMenuLoadPanel.UpdateLoadButtonState))]
    public static void MainMenuLoadPanelUpdateLoadButtonStatePostfix(MainMenuLoadButton lb)
    {
        using var log = SmartLog.ForAVS(RootModController.AnyInstance);
        try
        {
            // A SaveIcon should be square
            log.Debug(nameof(AddLoadButtonSprites));
            AddLoadButtonSprites(lb);

            log.Debug(nameof(GameObjectHelper.SafeSetActive));
            if (SaveLoadManagerPatcher.GetTechTypeGameInfo(lb.saveGame, out var hasTechTypes))
                foreach (var tt in hasTechTypes)
                {
                    log.Debug($"Processing {lb.NiceName()}");
                    log.Debug($"Processing {lb.saveIcons.NiceName()}");
                    var child = lb.saveIcons.FindChild(tt);
                    log.Debug($"Resolved tt '{tt}' to child {child.NiceName()}. Setting active");
                    child.SafeSetActive(true);
                }


            log.Debug("set spacing");
            lb.saveIcons.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().spacing = 0;

            log.Debug("count active");
            var count = 0;
            foreach (Transform tr in lb.saveIcons.transform)
                if (tr.gameObject.activeInHierarchy)
                    count++;

            log.Debug("set sizeDelta");
            foreach (Transform tr in lb.saveIcons.transform)
                if (count > 6)
                    tr.GetComponent<RectTransform>().sizeDelta *= 6 / (float)count;
        }
        catch (System.Exception e)
        {
            log.Error("postfix failed", e);
        }
    }
}