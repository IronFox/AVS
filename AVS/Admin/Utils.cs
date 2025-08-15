using AVS.Crafting;
using AVS.Localization;
using AVS.Log;
using AVS.UpgradeModules;
using AVS.UpgradeModules.Common;
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
        /// <summary>
        /// Determines whether the specified transform or any of its ancestors is the currently mounted vehicle.
        /// </summary>
        /// <remarks>This method recursively traverses the transform hierarchy to determine if any
        /// ancestor is the player's currently mounted vehicle. If the specified transform is <see langword="null"/>,
        /// the method returns <see langword="false"/>.</remarks>
        /// <param name="current">The transform to check, typically representing a game object in the hierarchy.</param>
        /// <param name="vehicle">When this method returns, contains the <see cref="Vehicle"/> component if found. Null if the method returns false</param>
        /// <param name="checkedAncestry">A list of all transforms visited by the recursion.</param>
        /// <returns><see langword="true"/> if the specified transform or one of its ancestors is the vehicle currently mounted
        /// by the player; otherwise, <see langword="false"/>.</returns>
        public static bool FindVehicleInParents(Transform current, out Vehicle? vehicle, List<Transform>? checkedAncestry = null)
        {
            if (!current)
            {
                vehicle = null;
                return false;
            }
            checkedAncestry?.Add(current);
            var vh = current.GetComponent<Vehicle>();
            if (vh)
            {
                vehicle = vh;
                return vh == Player.main.GetVehicle();
            }
            return FindVehicleInParents(current.parent, out vehicle, checkedAncestry);
        }
        /// <summary>
        /// Registers the common depth modules for vehicles.
        /// </summary>
        public static void RegisterDepthModules()
        {
            LogWriter.Default.Write(
                "Registering depth modules");
            var folder = Node.Create(
                MainPatcher.Instance.ClassPrefix + "DepthModules",
                Translator.Get(TranslationKey.Fabricator_Node_DepthModules),
                MainPatcher.Instance.DepthModuleNodeIcon);


            UpgradeCompat compat = UpgradeCompat.AvsVehiclesOnly;
            var depthmodule1 = new DepthModule1();
            folder.RegisterUpgrade(depthmodule1, compat);

            var depthmodule2 = new DepthModule2();
            depthmodule2.ExtendRecipe(depthmodule1);
            folder.RegisterUpgrade(depthmodule2, compat);

            var depthmodule3 = new DepthModule3();
            depthmodule3.ExtendRecipe(depthmodule2);
            folder.RegisterUpgrade(depthmodule3, compat);

            DepthModuleBase.AllDepthModuleTypes.AddRange(depthmodule1.TechTypes.AllNotNone);
            DepthModuleBase.AllDepthModuleTypes.AddRange(depthmodule2.TechTypes.AllNotNone);
            DepthModuleBase.AllDepthModuleTypes.AddRange(depthmodule3.TechTypes.AllNotNone);
            LogWriter.Default.Write(
                "Registered depth modules: " +
                string.Join(", ", DepthModuleBase.AllDepthModuleTypes.Select(x => x.ToString())));
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
                VehicleEntry ve = AvsVehicleManager.VehicleTypes.Where(x => x.name.Contains(name)).First();
                return ve.techType;
            }
            catch
            {
                Logger.Error("GetTechTypeFromVehicleName Error. Could not find a vehicle by the name: " + name + ". Here are all vehicle names:");
                AvsVehicleManager.VehicleTypes.ForEach(x => Logger.Log(x.name));
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
            MainPatcher.Instance.StartCoroutine(AddEncyclopediaEntryInternal());
        }
    }
}
