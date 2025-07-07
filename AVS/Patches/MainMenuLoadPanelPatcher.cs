using AVS.Util;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

// PURPOSE: allow custom save file sprites to be displayed
// VALUE: High.

namespace AVS.Patches
{
    // See also: SaveLoadManagerPatcher
    [HarmonyPatch(typeof(MainMenuLoadPanel))]
    public class MainMenuLoadPanelPatcher
    {
        public static List<string> HasTechTypes = new List<string>();

        public static void AddLoadButtonSprites(MainMenuLoadButton lb)
        {
            foreach (var ve in VehicleManager.vehicleTypes)
            {
                if (ve.mv != null && ve.mv.Config.SaveFileSprite != Assets.StaticAssets.DefaultSaveFileSprite)
                {
                    string techType = ve.techType.AsString();
                    GameObject imageObject = new GameObject(techType);
                    imageObject.transform.SetParent(lb.saveIcons.transform, false);
                    imageObject.AddComponent<UnityEngine.UI.Image>().sprite = ve.mv.Config.SaveFileSprite;
                    imageObject.EnsureComponent<RectTransform>().sizeDelta = new Vector2(24, 24);
                    imageObject.SetActive(false);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(MainMenuLoadPanel.UpdateLoadButtonState))]
        public static void MainMenuLoadPanelUpdateLoadButtonStatePostfix(MainMenuLoadButton lb)
        {
            // A SaveIcon should be square
            AddLoadButtonSprites(lb);

            if (SaveLoadManagerPatcher.hasTechTypeGameInfo.ContainsKey(lb.saveGame))
            {
                List<string> hasTechTypes = SaveLoadManagerPatcher.hasTechTypeGameInfo[lb.saveGame];
                hasTechTypes.ForEach(x => lb.saveIcons.FindChild(x).SafeSetActive(true));
            }

            lb.saveIcons.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().spacing = 0;

            int count = 0;
            foreach (Transform tr in lb.saveIcons.transform)
            {
                if (tr.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            foreach (Transform tr in lb.saveIcons.transform)
            {
                if (count > 6)
                {
                    tr.GetComponent<RectTransform>().sizeDelta *= (6 / (float)count);
                }
            }
        }
    }
}
