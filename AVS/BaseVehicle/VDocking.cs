using AVS.MaterialAdapt;
using System.Collections;
using UnityEngine;

namespace AVS.BaseVehicle
{
    public abstract partial class AvsVehicle
    {
        /// <summary>
        /// Gets a value indicating whether the undocking animation is currently in progress.
        /// </summary>
        public bool IsUndockingAnimating { get; internal set; } = false;

        /// <summary>
        /// True if the vehicle is currently docked in a docking bay (e.g. a moonpool).
        /// </summary>
        protected bool IsVehicleDocked { get; private set; } = false;

        /// <summary>
        /// Executed when the vehicle docks in a docking bay (e.g. a moonpool).
        /// </summary>
        /// <remarks>Calls <see cref="OnPlayerDocked(Vector3)" /> if the vessel is currently being controlled</remarks>
        /// <param name="exitLocation">
        /// The location the player should exit to after docking</param>
        public virtual void OnVehicleDocked(Vector3 exitLocation)
        {
            if (Config.AutoFixMaterials)
                MaterialFixer.OnVehicleDocked();
            // The Moonpool invokes this once upon vehicle entry into the dock
            IsVehicleDocked = true;
            if (IsUnderCommand)
                OnPlayerDocked(exitLocation);
            useRigidbody.detectCollisions = false;
            foreach (var component in GetComponentsInChildren<IDockListener>())
                component.OnDock();
        }

        /// <summary>
        /// Executed when the player should evict the vehicle after being docked in a docking bay (e.g. a moonpool).
        /// </summary>
        /// <remarks>Calls <see cref="ClosestPlayerExit" /></remarks>
        /// <param name="exitLocation">If non-zero, the player should relocate to this location</param>
        public virtual void OnPlayerDocked(Vector3 exitLocation)
        {
            ClosestPlayerExit(false);
            if (exitLocation != Vector3.zero)
            {
                Player.main.transform.position = exitLocation;
                Player.main.transform.LookAt(transform);
            }
        }

        /// <summary>
        /// Executed when the vehicle undocks from a docking bay (e.g. a moonpool).
        /// </summary>
        /// <remarks>Calls <see cref="OnPlayerUndocked()" /></remarks>
        public virtual void OnVehicleUndocked()
        {
            if (Config.AutoFixMaterials)
                MaterialFixer.OnVehicleUndocked();
            // The Moonpool invokes this once upon vehicle exit from the dock
            if (!isScuttled && !Admin.ConsoleCommands.isUndockConsoleCommand)
            {
                OnPlayerUndocked();
            }
            IsVehicleDocked = false;
            foreach (var component in GetComponentsInChildren<IDockListener>())
            {
                component.OnUndock();
            }
            IEnumerator EnsureCollisionsEnabledEventually()
            {
                yield return new WaitForSeconds(5f);
                useRigidbody.detectCollisions = true;
            }
            UWE.CoroutineHost.StartCoroutine(EnsureCollisionsEnabledEventually());
        }

        /// <summary>
        /// Executed when the player should reenter a newly undocked local vehicle.
        /// </summary>
        /// <remarks>Calls <see cref="ClosestPlayerEntry"/></remarks>
        public virtual void OnPlayerUndocked()
        {
            ClosestPlayerEntry();
        }
        /// <summary>
        /// Animation routine to execute when the vehicle is (un)docked in a moonpool.
        /// </summary>
        /// <param name="moonpool"></param>
        public virtual void AnimateMoonPoolArms(VehicleDockingBay moonpool)
        {
            // AnimateMoonPoolArms is called in VehicleDockingBay.LateUpdate when a ModVehicle is docked in a moonpool.
            // This line sets the arms of the moonpool to do exactly as they do for the seamoth
            // There is also "exosuit_docked"
            SafeAnimator.SetBool(moonpool.animator, "seamoth_docked", moonpool.vehicle_docked_param && moonpool.dockedVehicle != null);
        }


        /// <summary>
        /// Gets the difference between the vehicle's position and the center of its bounding box in world space,
        /// subject to its current orientation.
        /// </summary>
        public virtual Vector3 GetDockingDifferenceFromCenter()
        {
            var box = Com.BoundingBoxCollider;
            if (box != null)
            {
                Vector3 colliderCenterWorld = box.transform.TransformPoint(box.center);
                Vector3 difference = colliderCenterWorld - transform.position;
                return difference;
            }
            return Vector3.zero;
        }


        /// <summary>
        /// Loosely computes the bounding dimensions of the vehicle.
        /// </summary>
        public virtual Vector3 GetDockingBoundsSize()
        {
            var box = Com.BoundingBoxCollider;
            if (box == null)
            {
                return Vector3.zero;
            }
            Vector3 boxDimensions = box.size;
            Vector3 worldScale = box.transform.lossyScale;
            return Vector3.Scale(boxDimensions, worldScale);
        }

    }
}
