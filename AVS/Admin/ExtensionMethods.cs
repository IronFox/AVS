using AVS.Patches;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS
{
    /// <summary>
    /// Various extension methods for AVS functionality.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Queries the mod vehicle associated with the player.
        /// </summary>
        /// <param name="player">The player instance.</param>
        /// <returns>The <see cref="ModVehicle"/> associated with the player, or null if not found.</returns>
        public static ModVehicle? GetModVehicle(this Player player)
        {
            return
                player.GetVehicle() as ModVehicle
                ?? player.currentSub?.GetComponent<ModVehicle>();
        }

        /// <summary>
        /// Gets the list of current upgrade module names installed in the vehicle.
        /// </summary>
        /// <param name="vehicle">The vehicle instance.</param>
        /// <returns>A list of upgrade module names.</returns>
        public static List<string> GetCurrentUpgrades(this Vehicle vehicle)
        {
            return vehicle.modules.equipment.Select(x => x.Value).Where(x => x != null && x.item != null).Select(x => x.item.name).ToList();
        }

        /// <summary>
        /// Gets the list of current upgrade module names installed in all upgrade consoles of the subroot.
        /// </summary>
        /// <param name="subroot">The subroot instance.</param>
        /// <returns>A list of upgrade module names.</returns>
        public static List<string> GetCurrentUpgrades(this SubRoot subroot)
        {
            IEnumerable<string> upgrades = new List<string>();
            foreach (UpgradeConsole upgradeConsole in subroot.GetComponentsInChildren<UpgradeConsole>(true))
            {
                IEnumerable<string> theseUpgrades = upgradeConsole.modules.equipment.Select(x => x.Value).Where(x => x != null && x.item != null).Select(x => x.item.name).Where(x => x != string.Empty);
                upgrades = upgrades.Concat(theseUpgrades);
            }
            return upgrades.ToList();
        }

        /// <summary>
        /// Registers the audio source with the FreezeTimePatcher.
        /// </summary>
        /// <param name="source">The audio source to register.</param>
        /// <returns>The registered <see cref="AudioSource"/>.</returns>
        public static AudioSource Register(this AudioSource source)
        {
            return FreezeTimePatcher.Register(source);
        }

        /// <summary>
        /// Undocks the vehicle from its docking bay, if docked.
        /// </summary>
        /// <param name="vehicle">The vehicle to undock.</param>
        public static void Undock(this Vehicle vehicle)
        {
            void UndockModVehicle(Vehicle thisVehicle)
            {
                if (vehicle is ModVehicle mv)
                {
                    mv.OnVehicleUndocked();
                    vehicle.useRigidbody.detectCollisions = true;
                }
            }
            var theseBays = vehicle.transform.parent?.gameObject?.GetComponentsInChildren<VehicleDockingBay>()?.Where(x => x.dockedVehicle == vehicle);
            if (theseBays == null || theseBays.Count() == 0)
            {
                UndockModVehicle(vehicle);
                return;
            }
            VehicleDockingBay thisBay = theseBays.First();
            UWE.CoroutineHost.StartCoroutine(thisBay.MaybeToggleCyclopsCollision());
            thisBay.vehicle_docked_param = false;
            var toUndock = vehicle.liveMixin.IsAlive() && !Admin.ConsoleCommands.isUndockConsoleCommand
                ? Player.main
                : null;
            UWE.CoroutineHost.StartCoroutine(vehicle.Undock(toUndock, thisBay.transform.position.y));
            SkyEnvironmentChanged.Broadcast(vehicle.gameObject, (GameObject?)null);
            thisBay.dockedVehicle = null;
            UndockModVehicle(vehicle);
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
        public static bool IsPilotingCyclops(this Player player)
        {
            return player.IsInCyclops() && player.mode == Player.Mode.Piloting;
        }

        /// <summary>
        /// Determines if the player is currently inside a Cyclops.
        /// </summary>
        /// <param name="player">The player instance.</param>
        /// <returns>True if inside a Cyclops, otherwise false.</returns>
        public static bool IsInCyclops(this Player player)
        {
            return player.currentSub != null && player.currentSub.name.ToLower().Contains("cyclops");
        }

        /// <summary>
        /// Checks if the specified GameObject is an ancestor of the current transform.
        /// </summary>
        /// <param name="current">The current transform.</param>
        /// <param name="ancestor">The GameObject to check as ancestor.</param>
        /// <returns>True if ancestor is found, otherwise false.</returns>
        public static bool IsGameObjectAncestor(this Transform current, GameObject ancestor)
        {
            if (!current || !ancestor)
            {
                return false;
            }
            if (current.gameObject == ancestor)
            {
                return true;
            }
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
            {
                return TechType.None;
            }
            return vehicle.GetComponent<TechTag>().type;
        }
    }
}
