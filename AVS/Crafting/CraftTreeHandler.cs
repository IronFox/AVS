using AVS.Assets;
using AVS.Log;
using AVS.UpgradeModules;
using System;
using System.Collections.Generic;
//using AVS.Localization;

namespace AVS.Crafting
{
    internal static class CraftTreeHandler
    {
        //internal const string GeneralTabName = "AvsGeneral";

        /**
         * For consistency, tabs and modules must not exist in the same tab. Either a tab contains only tabs, or it contains only modules.
         * To assert consistency, we maintain these sets.
         */
        private class NodeInformation
        {
            public bool ContainsTabs { get; set; }
            public bool ContainsModules { get; set; }
        }


        private static Dictionary<Path<string>, NodeInformation> KnownPaths { get; } = new Dictionary<Path<string>, NodeInformation>();

        //internal static string ModuleRootNode(VehicleType type)
        //{
        //    switch (type)
        //    {
        //        case VehicleType.AvsVehicle:
        //            return "AvsCustom";
        //        default:
        //            return $"Avs{type}";
        //    }
        //}
        internal static void AddFabricatorMenus()
        {
            //var avsIcon = SpriteHelper.GetSpriteInternal("AvsUpgradesIcon.png") ?? StaticAssets.AvsVehicleIcon;
            //var generalIcon = StaticAssets.UpgradeIcon;
            //var seamothIcon = SpriteManager.Get(TechType.Seamoth) ?? StaticAssets.AvsVehicleIcon;
            //var prawnIcon = SpriteManager.Get(TechType.Exosuit) ?? StaticAssets.AvsVehicleIcon;
            //var cyclopsIcon = SpriteManager.Get(TechType.Cyclops) ?? StaticAssets.AvsVehicleIcon;

            //// add MV-universal tab
            //AddCraftingTab(Array.Empty<string>(), ModuleRootNode(VehicleType.AvsVehicle), Localization.Get(TranslationKey.CraftingTab_UniversalModules), vfIcon);
            //AddCraftingTab(ModuleRootNode(VehicleType.AvsVehicle).ToRoList(), $"{GeneralTabName}{VehicleType.AvsVehicle}", Language.main.Get("AvsGeneralTab"), mvIcon);
            // add MV-specific tab
            //AddCraftingTab(Array.Empty<string>(), ModuleRootNode(VehicleType.Custom), Translator.Get(TranslationKey.Fabricator_Node_Root), avsIcon);
            //AddCraftingTab(ModuleRootNode(VehicleType.Custom).ToRoList(), $"{GeneralTabName}{VehicleType.Custom}", Translator.Get(TranslationKey.Fabricator_Node_General), generalIcon);
            //// add seamoth tab
            //AddCraftingTab(Array.Empty<string>(), ModuleRootNode(VehicleType.Seamoth), Language.main.Get("AvsSeamothTab"), seamothIcon);
            //AddCraftingTab(ModuleRootNode(VehicleType.Seamoth).ToRoList(), $"{GeneralTabName}{VehicleType.Seamoth}", Language.main.Get("AvsGeneralTab"), mvIcon);
            //// add prawn tab
            //AddCraftingTab(Array.Empty<string>(), ModuleRootNode(VehicleType.Prawn), Language.main.Get("AvsPrawnTab"), prawnIcon);
            //AddCraftingTab(ModuleRootNode(VehicleType.Prawn).ToRoList(), $"{GeneralTabName}{VehicleType.Prawn}", Language.main.Get("AvsGeneralTab"), mvIcon);
            //// add cyclops tab
            //AddCraftingTab(Array.Empty<string>(), ModuleRootNode(VehicleType.Cyclops), Language.main.Get("AvsCyclopsTab"), cyclopsIcon);
            //AddCraftingTab(ModuleRootNode(VehicleType.Cyclops).ToRoList(), $"{GeneralTabName}{VehicleType.Cyclops}", Language.main.Get("AvsGeneralTab"), mvIcon);
        }
        internal static void EnsureCraftingTabsAvailable(AvsVehicleUpgrade upgrade, UpgradeCompat compat)
        {
            if (upgrade.IsVehicleSpecific)
            {
                EnsureCraftingTabAvailable(upgrade, VehicleType.Custom);
                return;
            }
            if (!compat.SkipCyclops)
            {
                EnsureCraftingTabAvailable(upgrade, VehicleType.Cyclops);
            }
            if (!compat.SkipExosuit)
            {
                EnsureCraftingTabAvailable(upgrade, VehicleType.Prawn);
            }
            if (!compat.SkipAvsVehicle)
            {
                EnsureCraftingTabAvailable(upgrade, VehicleType.AvsVehicle);
            }
            if (!compat.SkipSeamoth)
            {
                EnsureCraftingTabAvailable(upgrade, VehicleType.Seamoth);
            }
        }
        private static void EnsureCraftingTabAvailable(AvsVehicleUpgrade upgrade, VehicleType vType)
        {
            if (upgrade.TabPath == null)
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
                TraceCraftingPath(upgrade.TabPath.Value,
                    (x, y) => AddCraftingTab(x, y.DisplayName, y.Icon));
            }
        }

        internal static Path<string> TraceCraftingPath(
            Path<CraftingNode> path,
            Action<Path<string>, CraftingNode>? perNonLeafAction)
        {
            Path<string> thisPath = Path<string>.Empty;
            foreach (var node in path)
            {
                perNonLeafAction?.Invoke(thisPath, node);
                thisPath = thisPath.Append(node.Name);
            }
            return thisPath;
        }
        private static Path<string> AddCraftingTab(VehicleType vType, string tabName, string displayName, Atlas.Sprite? icon)
        {
            var path = new Path<string>(tabName);
            AddCraftingTab(path, displayName, icon);
            return path;
        }
        private static void AddCraftingTab(Path<string> tabPath, string displayName, Atlas.Sprite? icon)
        {
            if (!KnownPaths.ContainsKey(tabPath))
            {
                var parentPath = tabPath.Parent;
                LogWriter.Default.Write($"CraftTreeHandler: Adding crafting tab {tabPath.Last} with display name {displayName} in path {parentPath}");
                if (!parentPath.IsEmpty)
                {
                    if (KnownPaths.TryGetValue(parentPath, out var info))
                    {
                        if (info.ContainsModules)
                            throw new Exception($"CraftTreeHandler: Invalid Tab Path: {parentPath}. Cannot add a tab to a path that already contains modules.");
                        info.ContainsTabs = true;
                    }
                    else
                    {
                        KnownPaths.Add(parentPath, new NodeInformation { ContainsTabs = true });
                    }
                }
                Nautilus.Handlers.CraftTreeHandler.AddTabNode(AvsFabricator.TreeType, tabPath.Last, displayName, icon ?? StaticAssets.UpgradeIcon, parentPath.Segments);
                KnownPaths[tabPath] = new NodeInformation();
            }
        }

        internal static void RequireTabPathIsValidForModules(Path<string> tabPath, bool registerAsHavingModules)
        {
            if (!KnownPaths.TryGetValue(tabPath, out var info))
            {
                if (registerAsHavingModules)
                    KnownPaths.Add(tabPath, new NodeInformation { ContainsModules = true });
                return;
            }
            if (!info.ContainsTabs)
            {
                if (registerAsHavingModules)
                    info.ContainsModules = true;
                return;
            }
            throw new Exception($"CraftTreeHandler: Invalid Tab Path: {tabPath}. Cannot add a module to a path that already contains tabs.");
        }
    }
}
