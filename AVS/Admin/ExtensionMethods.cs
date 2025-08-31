using AVS.BaseVehicle;
using AVS.Patches;
using AVS.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS;

/// <summary>
/// Various extension methods for AVS functionality.
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Queries the vehicle associated with the player.
    /// </summary>
    /// <param name="player">The player instance.</param>
    /// <returns>The <see cref="AvsVehicle"/> associated with the player, or null if not found.</returns>
    public static AvsVehicle? GetAvsVehicle(this Player player)
    {
        return (player.GetVehicle() as AvsVehicle)
            .Or(() => player.currentSub.SafeGetComponent<AvsVehicle>());
    }

    /// <summary>
    /// Gets the list of current upgrade module names installed in the vehicle.
    /// </summary>
    /// <param name="vehicle">The vehicle instance.</param>
    /// <returns>A list of upgrade module names.</returns>
    public static List<string> GetCurrentUpgradeNames(this Vehicle vehicle)
    {
        return vehicle.modules.equipment.Select(x => x.Value).Where(x => x.IsNotNull() && x.item.IsNotNull())
            .Select(x => x.item.name).ToList();
    }

    /// <summary>
    /// Gets the list of current upgrade modules installed in the vehicle.
    /// </summary>
    /// <param name="vehicle">The vehicle instance.</param>
    /// <returns>A list of upgrade module names.</returns>
    public static List<Pickupable> GetCurrentUpgrades(this Vehicle vehicle)
    {
        return vehicle.modules.equipment.Select(x => x.Value).Where(x => x.IsNotNull() && x.item.IsNotNull())
            .Select(x => x.item).ToList();
    }


    /// <summary>
    /// Gets the list of current upgrade module names installed in all upgrade consoles of the subroot.
    /// </summary>
    /// <param name="subroot">The subroot instance.</param>
    /// <returns>A list of upgrade module names.</returns>
    public static List<string> GetCurrentUpgrades(this SubRoot subroot)
    {
        IEnumerable<string> upgrades = new List<string>();
        foreach (var upgradeConsole in subroot.GetComponentsInChildren<UpgradeConsole>(true))
        {
            var theseUpgrades = upgradeConsole.modules.equipment.Select(x => x.Value)
                .Where(x => x.IsNotNull() && x.item.IsNotNull()).Select(x => x.item.name).Where(x => x != string.Empty);
            upgrades = upgrades.Concat(theseUpgrades);
        }

        return upgrades.ToList();
    }

    /// <summary>
    /// Registers the audio source with the FreezeTimePatcher.
    /// </summary>
    /// <param name="source">The audio source to register.</param>
    /// <returns>The registered <see cref="AudioSource"/>.</returns>
    public static AudioSource Register(this AudioSource source) => FreezeTimePatcher.Register(source);

    /// <summary>
    /// Undocks the vehicle from its docking bay, if docked.
    /// </summary>
    /// <param name="vehicle">The vehicle to undock.</param>
    /// <param name="boardPlayer">Whether to integrate the player into the undocked vehicle (if AVS vehicle).</param>
    public static void Undock(this Vehicle vehicle, bool boardPlayer = true)
    {
        void UndockAvsVehicle(Vehicle thisVehicle)
        {
            if (vehicle is AvsVehicle av)
                av.UndockVehicle(boardPlayer, false);
        }

        var theseBays = vehicle.transform.parent
            .SafeGetGameObject()
            .SafeGetComponentsInChildren<VehicleDockingBay>()
            .Where(x => x.dockedVehicle == vehicle);
        // ReSharper disable once PossibleMultipleEnumeration
        if (theseBays.IsNull() || !theseBays.Any())
        {
            UndockAvsVehicle(vehicle);
            return;
        }

        // ReSharper disable once PossibleMultipleEnumeration
        var thisBay = theseBays.First();
        vehicle.StartCoroutine(thisBay.MaybeToggleCyclopsCollision());
        thisBay.vehicle_docked_param = false;
        var toUndock = vehicle.liveMixin.IsAlive() && !Admin.ConsoleCommands.isUndockConsoleCommand
            ? Player.main
            : null;
        vehicle.StartCoroutine(vehicle.Undock(toUndock, thisBay.transform.position.y));
        SkyEnvironmentChanged.Broadcast(vehicle.gameObject, (GameObject?)null);
        thisBay.dockedVehicle = null;
        UndockAvsVehicle(vehicle);
    }

    /// <summary>
    /// Coroutine to temporarily disable and re-enable Cyclops collision when undocking.
    /// </summary>
    /// <param name="bay">The docking bay instance.</param>
    /// <returns>An enumerator for the coroutine.</returns>
    public static IEnumerator MaybeToggleCyclopsCollision(this VehicleDockingBay bay)
    {
        if (bay.subRoot.name.ToLower().Contains("cyclops"))
        {
            bay.transform.parent.parent.parent.Find("CyclopsCollision").gameObject.SetActive(false);
            yield return new WaitForSeconds(2f);
            bay.transform.parent.parent.parent.Find("CyclopsCollision").gameObject.SetActive(true);
        }

        yield break;
    }

    /// <summary>
    /// Determines if the player is currently piloting a Cyclops.
    /// </summary>
    /// <param name="player">The player instance.</param>
    /// <returns>True if piloting a Cyclops, otherwise false.</returns>
    public static bool IsPilotingCyclops(this Player player) =>
        player.IsInCyclops() && player.mode == Player.Mode.Piloting;

    /// <summary>
    /// Determines if the player is currently inside a Cyclops.
    /// </summary>
    /// <param name="player">The player instance.</param>
    /// <returns>True if inside a Cyclops, otherwise false.</returns>
    public static bool IsInCyclops(this Player player) =>
        player.currentSub.IsNotNull() && player.currentSub.name.ToLower().Contains("cyclops");

    /// <summary>
    /// Checks if the specified GameObject is an ancestor of the current transform.
    /// </summary>
    /// <param name="current">The current transform.</param>
    /// <param name="ancestor">The GameObject to check as ancestor.</param>
    /// <returns>True if ancestor is found, otherwise false.</returns>
    public static bool IsGameObjectAncestor(this Transform current, GameObject ancestor)
    {
        if (!current || !ancestor)
            return false;
        if (current.gameObject == ancestor)
            return true;
        return current.parent.IsGameObjectAncestor(ancestor);
    }

    /// <summary>
    /// Gets the <see cref="TechType"/> of the vehicle.
    /// </summary>
    /// <param name="vehicle">The vehicle instance.</param>
    /// <returns>The <see cref="TechType"/> of the vehicle, or <see cref="TechType.None"/> if not found.</returns>
    public static TechType GetTechType(this Vehicle vehicle)
    {
        if (!vehicle || !vehicle.GetComponent<TechTag>())
            return TechType.None;
        return vehicle.GetComponent<TechTag>().type;
    }
}