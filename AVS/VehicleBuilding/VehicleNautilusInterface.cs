using AVS.Assets;
using AVS.Configuration;
using AVS.Util;
using Nautilus.Assets.Gadgets;
using System.IO;
using System.Reflection;

namespace AVS
{
    internal static class VehicleNautilusInterface
    {
        internal static TechType RegisterVehicle(VehicleEntry vehicle)
        {
            string vehicleKey = vehicle.mv.name;
            Nautilus.Assets.PrefabInfo vehicle_info = Nautilus.Assets.PrefabInfo.WithTechType(vehicleKey, vehicleKey, vehicle.mv.Config.Description);
            vehicle_info.WithIcon(vehicle.mv.Config.CraftingSprite ?? StaticAssets.ModVehicleIcon);

            Nautilus.Assets.CustomPrefab module_CustomPrefab = new Nautilus.Assets.CustomPrefab(vehicle_info);
            Nautilus.Utility.PrefabUtils.AddBasicComponents(vehicle.mv.VehicleRoot, vehicleKey, vehicle_info.TechType, LargeWorldEntity.CellLevel.Global);
            module_CustomPrefab.SetGameObject(vehicle.mv.VehicleRoot);
            string jsonRecipeFileName = Path.Combine(
                                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                            "recipes",
                                            $"{vehicleKey}_recipe.json");
            Nautilus.Crafting.RecipeData vehicleRecipe = Nautilus.Utility.JsonUtils.Load<Nautilus.Crafting.RecipeData>(jsonRecipeFileName, false, new Nautilus.Json.Converters.CustomEnumConverter());
            if (vehicleRecipe.Ingredients.Count == 0)
            {
                // If the custom recipe file doesn't exist, go ahead and make it using the default recipe.
                vehicleRecipe = vehicle.mv.Config.Recipe.ToRecipeData();
                Nautilus.Utility.JsonUtils.Save<Nautilus.Crafting.RecipeData>(vehicleRecipe, jsonRecipeFileName, new Nautilus.Json.Converters.CustomEnumConverter());
            }
            else if (vehicle.mv.Config.AllowRecipeOverride)
            {
                vehicleRecipe = vehicle.mv.OnRecipeOverride(
                    Recipe.Import(vehicleRecipe, vehicle.mv.Config.Recipe)
                    ).ToRecipeData();
                Nautilus.Utility.JsonUtils.Save<Nautilus.Crafting.RecipeData>(vehicleRecipe, jsonRecipeFileName, new Nautilus.Json.Converters.CustomEnumConverter());
            }
            else
            {
                vehicleRecipe = vehicle.mv.Config.Recipe.ToRecipeData();
                Nautilus.Utility.JsonUtils.Save<Nautilus.Crafting.RecipeData>(vehicleRecipe, jsonRecipeFileName, new Nautilus.Json.Converters.CustomEnumConverter());
            }

            module_CustomPrefab.SetRecipe(vehicleRecipe).WithFabricatorType(CraftTree.Type.Constructor).WithStepsToFabricatorTab(new string[] { "Vehicles" });
            var scanningGadget = module_CustomPrefab.SetUnlock(vehicle.mv.Config.UnlockedWith)
                .WithPdaGroupCategory(TechGroup.Constructor, TechCategory.Constructor);

            if (!string.IsNullOrEmpty(vehicle.mv.Config.EncyclopediaEntry))
            {
                Nautilus.Handlers.LanguageHandler.SetLanguageLine($"Ency_{vehicleKey}", vehicleKey);
                Nautilus.Handlers.LanguageHandler.SetLanguageLine($"EncyDesc_{vehicleKey}", vehicle.mv.Config.EncyclopediaEntry);
                scanningGadget.WithEncyclopediaEntry("Tech/Vehicles", null, vehicle.mv.Config.EncyclopediaImage.GetTexture2D());
                Nautilus.Handlers.StoryGoalHandler.RegisterItemGoal(vehicleKey, Story.GoalType.Encyclopedia, vehicle.mv.Config.UnlockedWith);
            }

            if (vehicle.mv.Config.UnlockedSprite != null)
            {
                scanningGadget.WithAnalysisTech(vehicle.mv.Config.UnlockedSprite, unlockMessage: vehicle.mv.Config.UnlockedMessage);
            }
            module_CustomPrefab.Register();
            return vehicle_info.TechType;
        }

        internal static void PatchCraftable(ref VehicleEntry ve, bool verbose)
        {
            try
            {
                TechType techType = VehicleNautilusInterface.RegisterVehicle(ve);
                VehicleRegistrar.VerboseLog(VehicleRegistrar.LogType.Log, verbose, $"Patched the {ve.name} Craftable");
                VehicleEntry newVE = new VehicleEntry(ve.mv, ve.unique_id, ve.pt, ve.ping_sprite, techType);
                VehicleManager.vehicleTypes.Add(newVE);
            }
            catch (System.Exception e)
            {
                Logger.LogException($"VehicleNautilusInterface Error: Failed to Register Vehicle {ve.name}. Error follows:", e);
                Logger.LoopMainMenuError($"Failed registration. See log.", ve.name);
            }
        }
    }
}
