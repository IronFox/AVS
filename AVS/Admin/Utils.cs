using AVS.UpgradeTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.Admin
{
    /// <summary>
    /// Global utility methods for the AVS mod.
    /// </summary>
    public static class Utils
    {


        //public static void LoadShader(ModVehicle mv, Shader shade)
        //{
        //    VehicleBuilder.ApplyShaders(mv, shade);
        //}

        /// <summary>
        /// Determines whether the specified transform or any of its ancestors is the currently mounted vehicle.
        /// </summary>
        /// <remarks>This method recursively traverses the transform hierarchy to determine if any
        /// ancestor is the player's currently mounted vehicle. If the specified transform is <see langword="null"/>,
        /// the method returns <see langword="false"/>.</remarks>
        /// <param name="current">The transform to check, typically representing a game object in the hierarchy.</param>
        /// <returns><see langword="true"/> if the specified transform or one of its ancestors is the vehicle currently mounted
        /// by the player; otherwise, <see langword="false"/>.</returns>
        public static bool IsAnAncestorTheCurrentMountedVehicle(Transform current)
        {
            if (!current)
            {
                return false;
            }
            var vh = current.GetComponent<Vehicle>();
            if (vh)
            {
                return vh == Player.main.GetVehicle();
            }
            return IsAnAncestorTheCurrentMountedVehicle(current.parent);
        }
        /// <summary>
        /// Registers the common depth modules for vehicles.
        /// </summary>
        public static void RegisterDepthModules()
        {
            UpgradeCompat compat = new UpgradeCompat
            {
                skipModVehicle = false,
                skipCyclops = true,
                skipSeamoth = true,
                skipExosuit = true
            };
            UpgradeTechTypes depth1 = UpgradeRegistrar.RegisterUpgrade(new DepthModules.DepthModule1(), compat);

            var depthmodule2 = new DepthModules.DepthModule2();
            depthmodule2.ExtendRecipe(depth1);
            UpgradeTechTypes depth2 = UpgradeRegistrar.RegisterUpgrade(depthmodule2, compat);

            var depthmodule3 = new DepthModules.DepthModule3();
            depthmodule3.ExtendRecipe(depth2);
            UpgradeTechTypes depth3 = UpgradeRegistrar.RegisterUpgrade(depthmodule3, compat);
        }


        /// <summary>
        /// Evaluates the depth upgrade modules installed on the specified vehicle and adjusts its crush depth
        /// accordingly.
        /// </summary>
        /// <remarks>This method checks the installed depth upgrade modules on the provided vehicle and
        /// determines the highest level of depth module present. Based on the detected module level, it calculates the
        /// additional crush depth and applies it to the vehicle. If the vehicle is not compatible with depth upgrades,
        /// a message is displayed to the user.</remarks>
        /// <param name="param">The parameters containing the vehicle to evaluate and its associated data.</param>
        public static void EvaluateDepthModules(AddActionParams param)
        {
            ModVehicle mv = param.vehicle.GetComponent<ModVehicle>();
            if (!mv)
            {
                Subtitles.Add("This upgrade is not compatible with this vehicle.");
                return;
            }
            // Iterate over all upgrade modules,
            // in order to determine our max depth module level
            int maxDepthModuleLevel = 0;
            List<string> upgrades = mv.GetCurrentUpgrades();
            foreach (var upgrade in upgrades)
            {
                if (string.Equals(upgrade, "AvsDepthModule1(Clone)", StringComparison.OrdinalIgnoreCase))
                {
                    maxDepthModuleLevel = maxDepthModuleLevel < 1 ? 1 : maxDepthModuleLevel;
                }
                else if (string.Equals(upgrade, "AvsDepthModule2(Clone)", StringComparison.OrdinalIgnoreCase))
                {
                    maxDepthModuleLevel = maxDepthModuleLevel < 2 ? 2 : maxDepthModuleLevel;
                }
                else if (string.Equals(upgrade, "AvsDepthModule3(Clone)", StringComparison.OrdinalIgnoreCase))
                {
                    maxDepthModuleLevel = maxDepthModuleLevel < 3 ? 3 : maxDepthModuleLevel;
                }
            }
            int extraDepthToAdd = 0;
            extraDepthToAdd = maxDepthModuleLevel > 0 ? extraDepthToAdd += mv.Config.CrushDepthUpgrade1 : extraDepthToAdd;
            extraDepthToAdd = maxDepthModuleLevel > 1 ? extraDepthToAdd += mv.Config.CrushDepthUpgrade2 : extraDepthToAdd;
            extraDepthToAdd = maxDepthModuleLevel > 2 ? extraDepthToAdd += mv.Config.CrushDepthUpgrade3 : extraDepthToAdd;
            mv.GetComponent<CrushDamage>().SetExtraCrushDepth(extraDepthToAdd);
        }


        /// <summary>
        /// Retrieves the <see cref="TechType"/> associated with a vehicle based on its name.
        /// </summary>
        /// <remarks>If no vehicle with the specified name is found, an error is logged, and the method
        /// returns <see cref="TechType.None"/>.</remarks>
        /// <param name="name">The name of the vehicle to search for. This parameter is case-sensitive and must not be null or empty.</param>
        /// <returns>The <see cref="TechType"/> of the vehicle if a match is found; otherwise, returns <see
        /// cref="TechType.None"/>.</returns>
        public static TechType GetTechTypeFromVehicleName(string name)
        {
            try
            {
                VehicleEntry ve = VehicleManager.vehicleTypes.Where(x => x.name.Contains(name)).First();
                return ve.techType;
            }
            catch
            {
                Logger.Error("GetTechTypeFromVehicleName Error. Could not find a vehicle by the name: " + name + ". Here are all vehicle names:");
                VehicleManager.vehicleTypes.ForEach(x => Logger.Log(x.name));
                return 0;
            }
        }


        /// <summary>
        /// Adds a new entry to the PDA Encyclopedia or updates an existing one if the key already exists.
        /// </summary>
        /// <remarks>This method ensures that the entry is added or updated only after the PDA
        /// Encyclopedia mapping is initialized.  If an entry with the same key already exists, it will be replaced with
        /// the provided data.</remarks>
        /// <param name="data">The encyclopedia entry data to add or update. The <see cref="PDAEncyclopedia.EntryData.key"/> property must
        /// be unique and non-null.</param>
        public static void AddEncyclopediaEntry(PDAEncyclopedia.EntryData data)
        {
            IEnumerator AddEncyclopediaEntryInternal()
            {
                yield return new WaitUntil(() => PDAEncyclopedia.mapping != null);
                if (PDAEncyclopedia.mapping.ContainsKey(data.key))
                {
                    PDAEncyclopedia.mapping[data.key] = data;
                }
                else
                {
                    PDAEncyclopedia.mapping.Add(data.key, data);
                }
            }
            UWE.CoroutineHost.StartCoroutine(AddEncyclopediaEntryInternal());
        }
    }
}
