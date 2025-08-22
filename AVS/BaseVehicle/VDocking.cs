using AVS.MaterialAdapt;
using System;
using System.Collections;
using AVS.Util;
using UnityEngine;

namespace AVS.BaseVehicle;

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
    /// Handles docking procedures for the vehicle.
    /// Executed when the vehicle docks in a docking bay (e.g. a moonpool).
    /// Relocates the player if they are currently controlling the vehicle,
    /// </summary>
    /// <remarks>
    /// Updates <see cref="IsVehicleDocked"/> and <see cref="Vehicle.docked"/>.
    /// Calls <see cref="OnPreVehicleDocked()"/>,
    /// <see cref="OnVehicleDocked()"/>,
    /// and potentially <see cref="OnPreDockingPlayerExit()"/>
    /// and <see cref="OnDockingPlayerExit()"/>.
    /// </remarks>
    /// <param name="doNotRelocatePlayer">
    /// If set, the player will not be relocated to the exit hatch of the vehicle.
    /// Also <see cref="OnPreDockingPlayerExit"/> and <see cref="OnDockingPlayerExit"/> will not be called.
    /// This is a very crude option and requires that the caller ensures the player
    /// is properly relocated, and all states (e.g. swimming, sitting) are properly updated.
    /// </param>
    /// <param name="overridePlayerExitLocation">
    /// The location the player should exit to after docking.
    /// If <see cref="Vector3.zero" /> / <see langword="default"/>,
    /// the player will only relocate to the exit location of the respective hatch</param>
    public void DockVehicle(Vector3 overridePlayerExitLocation = default,
        bool doNotRelocatePlayer = false)
    {
        SafeSignal(OnPreVehicleDocked, nameof(OnPreVehicleDocked));
        docked = true;

        if (Config.AutoFixMaterials)
            MaterialFixer.OnVehicleDocked();
        // The Moonpool invokes this once upon vehicle entry into the dock
        IsVehicleDocked = true;
        if (!doNotRelocatePlayer)
        {
            if (IsBoarded)
            {
                SafeSignal(OnPreDockingPlayerExit, nameof(OnPreDockingPlayerExit));
                ClosestPlayerExit(false);
                if (overridePlayerExitLocation != Vector3.zero)
                {
                    Player.main.transform.position = overridePlayerExitLocation;
                    Player.main.transform.LookAt(transform);
                }

                SafeSignal(OnDockingPlayerExit, nameof(OnDockingPlayerExit));
            }
        }
        else
        {
            IsBoarded = false; // If we do not relocate the player, we should not be boarded
        }

        useRigidbody.detectCollisions = false;
        foreach (var component in GetComponentsInChildren<IDockListener>())
            component.OnDock();

        SafeSignal(OnVehicleDocked, nameof(OnVehicleDocked));
    }

    private void SafeSignal(Action action, string actionName)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Log.Error($"Error while executing {actionName} for {GetType().Name}: {e}");
        }
    }

    /// <summary>
    /// Called before the vehicle is docked in a docking bay (e.g. a moonpool).
    /// </summary>
    protected virtual void OnPreVehicleDocked()
    {
    }

    /// <summary>
    /// Called after the vehicle handled docking procedures
    /// and potentially relocated the player.
    /// </summary>
    protected virtual void OnVehicleDocked()
    {
    }

    /// <summary>
    /// Called before the player exits the vehicle in a docking bay (e.g. a moonpool).
    /// </summary>
    protected virtual void OnPreDockingPlayerExit()
    {
    }

    /// <summary>
    /// Called after the player has exited the vehicle in a docking bay (e.g. a moonpool).
    /// </summary>
    protected virtual void OnDockingPlayerExit()
    {
    }

    /// <summary>
    /// Handles undocking procedures for the vehicle.
    /// Executed when the vehicle undocks from a docking bay (e.g. a moonpool).
    /// </summary>
    /// <remarks>
    /// Updates <see cref="IsVehicleDocked"/> and <see cref="Vehicle.docked"/>.
    /// Calls <see cref="OnPreVehicleUndocked()"/>,
    /// <see cref="OnVehicleUndocked()"/>, and potentially
    /// <see cref="OnPreUndockingPlayerEntry()"/> and
    /// <see cref="OnUndockingPlayerEntry()"/>.
    /// </remarks>
    /// <param name="boardPlayer">Whether to board the player into this vehicle</param>
    /// <param name="suspendCollisions">
    /// If false, the vehicle's rigidbody will immediately enable collisions after undocking.
    /// Otherwise, it will disable collisions, then wait 5 seconds before enabling collisions.
    /// </param>
    public void UndockVehicle(bool boardPlayer = true, bool suspendCollisions = true)
    {
        SafeSignal(OnPreVehicleUndocked, nameof(OnPreVehicleUndocked));
        if (Config.AutoFixMaterials)
            MaterialFixer.OnVehicleUndocked();
        docked = false;

        // The Moonpool invokes this once upon vehicle exit from the dock
        if (!isScuttled && !Admin.ConsoleCommands.isUndockConsoleCommand && boardPlayer)
        {
            SafeSignal(OnPreUndockingPlayerEntry, nameof(OnPreUndockingPlayerEntry));

            ClosestPlayerEntry();

            SafeSignal(OnUndockingPlayerEntry, nameof(OnUndockingPlayerEntry));
        }

        IsVehicleDocked = false;
        foreach (var component in GetComponentsInChildren<IDockListener>())
            component.OnUndock();
        if (!suspendCollisions)
        {
            useRigidbody.detectCollisions = true;
        }
        else
        {
            useRigidbody.detectCollisions = false;

            IEnumerator EnsureCollisionsEnabledEventually()
            {
                yield return new WaitForSeconds(5f);
                useRigidbody.detectCollisions = true;
            }

            MainPatcher.Instance.StartCoroutine(EnsureCollisionsEnabledEventually());
        }

        SafeSignal(OnVehicleUndocked, nameof(OnVehicleUndocked));
    }

    /// <summary>
    /// Executed before the vehicle is undocked from a docking bay (e.g. a moonpool).
    /// </summary>
    protected virtual void OnPreVehicleUndocked()
    {
    }

    /// <summary>
    /// Executed when the vehicle has undocking from a docking bay (e.g. a moonpool).
    /// </summary>
    protected virtual void OnVehicleUndocked()
    {
    }

    /// <summary>
    /// Executed before the player reenters a newly undocked local vehicle.
    /// </summary>
    protected virtual void OnPreUndockingPlayerEntry()
    {
    }

    /// <summary>
    /// Executed when the player reentered a newly undocked local vehicle.
    /// </summary>
    protected virtual void OnUndockingPlayerEntry()
    {
    }

    /// <summary>
    /// Animation routine to execute when the vehicle is (un)docked in a moonpool.
    /// </summary>
    /// <param name="moonpool"></param>
    public virtual void AnimateMoonPoolArms(VehicleDockingBay moonpool)
    {
        // AnimateMoonPoolArms is called in VehicleDockingBay.LateUpdate when a AvsVehicle is docked in a moonpool.
        // This line sets the arms of the moonpool to do exactly as they do for the seamoth
        // There is also "exosuit_docked"
        SafeAnimator.SetBool(moonpool.animator, "seamoth_docked",
            moonpool.vehicle_docked_param && moonpool.dockedVehicle.IsNotNull());
    }


    /// <summary>
    /// Gets the difference between the vehicle's position and the center of its bounding box in world space,
    /// subject to its current orientation.
    /// </summary>
    public virtual Vector3 GetDockingDifferenceFromCenter()
    {
        var box = Com.BoundingBoxCollider;
        if (box.IsNotNull())
        {
            var colliderCenterWorld = box.transform.TransformPoint(box.center);
            var difference = colliderCenterWorld - transform.position;
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
        if (box.IsNull())
            return Vector3.zero;
        var boxDimensions = box.size;
        var worldScale = box.transform.lossyScale;
        return Vector3.Scale(boxDimensions, worldScale);
    }
}