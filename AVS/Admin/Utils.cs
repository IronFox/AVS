using AVS.Crafting;
using AVS.Localization;
using AVS.Log;
using AVS.UpgradeModules;
using AVS.UpgradeModules.Common;
using AVS.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.Admin;

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
    public static bool FindVehicleInParents(Transform current, out Vehicle? vehicle,
        List<Transform>? checkedAncestry = null)
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
    public static void RegisterDepthModules(RootModController rmc)
    {
        using var log = SmartLog.ForAVS(rmc);
        log.Write(
            "Registering depth modules");
        var folder = Node.Create(
            rmc.ModName + "DepthModules",
            Translator.Get(TranslationKey.Fabricator_Node_DepthModules),
            rmc.DepthModuleNodeIcon);


        var compat = UpgradeCompat.AvsVehiclesOnly;
        var depthmodule1 = new DepthModule1(rmc);
        folder.RegisterUpgrade(depthmodule1, compat);

        var depthmodule2 = new DepthModule2(rmc);
        depthmodule2.ExtendRecipe(depthmodule1);
        folder.RegisterUpgrade(depthmodule2, compat);

        var depthmodule3 = new DepthModule3(rmc);
        depthmodule3.ExtendRecipe(depthmodule2);
        folder.RegisterUpgrade(depthmodule3, compat);

        DepthModuleBase.AllDepthModuleTypes.AddRange(depthmodule1.TechTypes.AllNotNone);
        DepthModuleBase.AllDepthModuleTypes.AddRange(depthmodule2.TechTypes.AllNotNone);
        DepthModuleBase.AllDepthModuleTypes.AddRange(depthmodule3.TechTypes.AllNotNone);
        log.Write(
            "Registered depth modules: " +
            string.Join(", ", DepthModuleBase.AllDepthModuleTypes.Select(x => x.ToString())));
    }




    /// <summary>
    /// Adds a new entry to the PDA Encyclopedia or updates an existing one if the key already exists.
    /// </summary>
    /// <remarks>This method ensures that the entry is added or updated only after the PDA
    /// Encyclopedia mapping is initialized.  If an entry with the same key already exists, it will be replaced with
    /// the provided data.</remarks>
    /// <param name="rmc">The <see cref="RootModController"/> instance owning the process.</param>
    /// <param name="data">The encyclopedia entry data to add or update. The <see cref="PDAEncyclopedia.EntryData.key"/> property must
    /// be unique and non-null.</param>
    public static void AddEncyclopediaEntry(RootModController rmc, PDAEncyclopedia.EntryData data)
    {
        IEnumerator AddEncyclopediaEntryInternal()
        {
            yield return new WaitUntil(() => PDAEncyclopedia.mapping.IsNotNull());
            PDAEncyclopedia.mapping[data.key] = data;
        }

        rmc.StartCoroutine(AddEncyclopediaEntryInternal());
    }
}