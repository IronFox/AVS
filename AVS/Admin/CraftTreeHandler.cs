using AVS.Assets;
using AVS.UpgradeTypes;
using AVS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
//using AVS.Localization;

namespace AVS.Admin
{
    internal static class CraftTreeHandler
    {
        internal const string GeneralTabName = "AvsGeneral";
        private readonly static List<IReadOnlyList<string>> KnownPaths = new List<IReadOnlyList<string>>();
        internal readonly static List<string> TabNodeTabNodes = new List<string>();
        internal readonly static List<string> CraftNodeTabNodes = new List<string>();
        internal static string ModuleRootNode(VehicleType type)
        {
            switch (type)
            {
                case VehicleType.AvsVehicle:
                    return "AvsUniversal";
                default:
                    return $"Avs{type}";
            }
        }
        internal static void AddFabricatorMenus()
        {
            var vfIcon = SpriteHelper.GetSpriteInternal("AvsUpgradesIcon.png") ?? StaticAssets.AvsVehicleIcon;
            var mvIcon = StaticAssets.UpgradeIcon;
            var seamothIcon = SpriteManager.Get(TechType.Seamoth) ?? StaticAssets.AvsVehicleIcon;
            var prawnIcon = SpriteManager.Get(TechType.Exosuit) ?? StaticAssets.AvsVehicleIcon;
            var cyclopsIcon = SpriteManager.Get(TechType.Cyclops) ?? StaticAssets.AvsVehicleIcon;

            // add MV-universal tab
            AddCraftingTab(Array.Empty<string>(), ModuleRootNode(VehicleType.AvsVehicle), Language.main.Get("AvsMvModules"), vfIcon);
            AddCraftingTab(ModuleRootNode(VehicleType.AvsVehicle).ToRoList(), $"{GeneralTabName}{VehicleType.AvsVehicle}", Language.main.Get("AvsGeneralTab"), mvIcon);
            // add MV-specific tab
            AddCraftingTab(Array.Empty<string>(), ModuleRootNode(VehicleType.Custom), Language.main.Get("AvsSpecificModules"), vfIcon);
            AddCraftingTab(ModuleRootNode(VehicleType.Custom).ToRoList(), $"{GeneralTabName}{VehicleType.Custom}", Language.main.Get("AvsGeneralTab"), mvIcon);
            // add seamoth tab
            AddCraftingTab(Array.Empty<string>(), ModuleRootNode(VehicleType.Seamoth), Language.main.Get("AvsSeamothTab"), seamothIcon);
            AddCraftingTab(ModuleRootNode(VehicleType.Seamoth).ToRoList(), $"{GeneralTabName}{VehicleType.Seamoth}", Language.main.Get("AvsGeneralTab"), mvIcon);
            // add prawn tab
            AddCraftingTab(Array.Empty<string>(), ModuleRootNode(VehicleType.Prawn), Language.main.Get("AvsPrawnTab"), prawnIcon);
            AddCraftingTab(ModuleRootNode(VehicleType.Prawn).ToRoList(), $"{GeneralTabName}{VehicleType.Prawn}", Language.main.Get("AvsGeneralTab"), mvIcon);
            // add cyclops tab
            AddCraftingTab(Array.Empty<string>(), ModuleRootNode(VehicleType.Cyclops), Language.main.Get("AvsCyclopsTab"), cyclopsIcon);
            AddCraftingTab(ModuleRootNode(VehicleType.Cyclops).ToRoList(), $"{GeneralTabName}{VehicleType.Cyclops}", Language.main.Get("AvsGeneralTab"), mvIcon);
        }
        internal static void EnsureCraftingTabsAvailable(AvsVehicleUpgrade upgrade, UpgradeCompat compat)
        {
            if (upgrade.IsVehicleSpecific)
            {
                EnsureCraftingTabAvailable(upgrade, VehicleType.Custom);
                return;
            }
            if (!compat.skipCyclops)
            {
                EnsureCraftingTabAvailable(upgrade, VehicleType.Cyclops);
            }
            if (!compat.skipExosuit)
            {
                EnsureCraftingTabAvailable(upgrade, VehicleType.Prawn);
            }
            if (!compat.skipAvsVehicle)
            {
                EnsureCraftingTabAvailable(upgrade, VehicleType.AvsVehicle);
            }
            if (!compat.skipSeamoth)
            {
                EnsureCraftingTabAvailable(upgrade, VehicleType.Seamoth);
            }
        }
        private static void EnsureCraftingTabAvailable(AvsVehicleUpgrade upgrade, VehicleType vType)
        {
            if (upgrade.CraftingPath == null)
            {
                if (upgrade.TabName.Equals(string.Empty))
                {
                    // it goes in general, so we're good
                    return;
                }
                else
                {
                    AddCraftingTab(vType, upgrade.TabName, upgrade.TabDisplayName, upgrade.TabIcon);
                }
            }
            else
            {
                TraceCraftingPath(vType, upgrade.CraftingPath, (x, y) => AddCraftingTab(x, y.Name, y.DisplayName, y.Icon));
            }
        }

        /// <summary>
        /// Iterates through the crafting path and constructs the full path.
        /// </summary>
        /// <param name="vType">Type determining the root node</param>
        /// <param name="path">Crafting nodes to append to the root node</param>
        /// <param name="perNonLeafAction">Optional action to execute for every node except the final one</param>
        /// <returns>Complete path</returns>
        internal static IReadOnlyList<string> TraceCraftingPath(VehicleType vType, IReadOnlyList<CraftingNode> path, Action<IReadOnlyList<string>, CraftingNode>? perNonLeafAction)
        {
            List<string> pathList = new List<string>
            {
                ModuleRootNode(vType)
            };
            foreach (var node in path)
            {
                perNonLeafAction?.Invoke(pathList, node);
                pathList.Add(node.Name);
            }
            return pathList;
        }
        private static IReadOnlyList<string> AddCraftingTab(VehicleType vType, string tabName, string displayName, Atlas.Sprite? icon)
        {
            return AddCraftingTab(new string[] { ModuleRootNode(vType) }, tabName, displayName, icon);
        }
        private static IReadOnlyList<string> AddCraftingTab(IReadOnlyList<string> thisPath, string tabName, string displayName, Atlas.Sprite? icon)
        {
            List<string> resultPath = new List<string>(thisPath)
            {
                tabName
            };
            if (!IsKnownPath(resultPath))
            {
                if (thisPath.Count > 0 && !IsValidTabPath(thisPath))
                {
                    throw new Exception($"CraftTreeHandler: Invalid Tab Path: there were crafting nodes in that tab: {thisPath.Last()}. Cannot mix tab nodes and crafting nodes.");
                }
                Nautilus.Handlers.CraftTreeHandler.AddTabNode(AVSFabricator.TreeType, tabName, displayName, icon ?? StaticAssets.UpgradeIcon, thisPath.ToArray());
                if (thisPath.Count > 0)
                {
                    TabNodeTabNodes.Add(thisPath.Last());
                }
                KnownPaths.Add(resultPath);
            }
            return resultPath;
        }
        private static bool IsKnownPath(IReadOnlyList<string> path)
            => KnownPaths.Any(x => x.CollectionsEqual(path));

        internal static bool IsValidTabPath(IReadOnlyList<string> steps)
        {
            // return false only if this tab has crafting nodes
            return !CraftNodeTabNodes.Contains(steps.Last());
        }
        internal static bool IsValidCraftPath(IReadOnlyList<string> steps)
        {
            // return false only if this tab has tab nodes
            return !TabNodeTabNodes.Contains(steps.Last());
        }
    }
}
