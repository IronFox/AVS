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
        //private class NodeInformation
        //{
        //    public bool ContainsTabs { get; set; }
        //    public bool ContainsModules { get; set; }
        //}


        private static Dictionary<string, Node> KnownRootNodes { get; } = new Dictionary<string, Node>();

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
        internal static Node GetOrCreateNode(
            string name,
            Node? parent,
            Func<Node> folderFactory)
        {

            Node? existing = null;
            if (parent is null)
            {
                if (KnownRootNodes.TryGetValue(name, out existing))
                    return existing;
            }
            else
            {
                if (parent.Children.TryGetValue(name, out existing))
                    return existing;
            }

            if (parent != null)
            {
                if (parent.Modules.Count > 0)
                    throw new InvalidOperationException($"CraftTreeHandler: Cannot add a folder to a path that already contains modules. Path: {parent.GetPath()}");
            }
            var folder = folderFactory();
            var parentPath = (parent?.GetPath() ?? CraftPath.Empty);

            LogWriter.Default.Write($"CraftTreeHandler: Adding crafting tab {name} with display name {folder.DisplayName} in path {parentPath}");
            Nautilus.Handlers.CraftTreeHandler.AddTabNode(AvsFabricator.TreeType, name, folder.DisplayName, folder.Icon, parentPath.Segments);

            if (parent != null)
                parent.Children.Add(name, folder);
            else
                KnownRootNodes.Add(name, folder);
            return folder;
        }

    }
}
