using AVS.Configuration;
using AVS.Util;
using AVS.VehicleBuilding;
using System;
using UnityEngine;

namespace AVS.BaseVehicle;

public abstract partial class AvsVehicle
{
    /// <summary>
    /// True if the player is currently piloting the vehicle.
    /// </summary>
    public Helm? PlayerAtHelm { get; private set; } = null;

    private void MyExitLockedMode()
    {
        GameInput.ClearInput();
        Player.main.transform.parent = null;
        Player.main.transform.localScale = Vector3.one;
        Player.main.currentMountedVehicle = null;
        Player.main.playerController.SetEnabled(true);
        Player.main.mode = Player.Mode.Normal;
        Player.main.playerModeChanged.Trigger(Player.main.mode);
        Player.main.sitting = false;
        Player.main.playerController.ForceControllerSize();
    }

    internal void DoCommonExitActions(ref Player.Mode mode)
    {
        var myPlayer = Player.main;
        GameInput.ClearInput();
        myPlayer.playerController.SetEnabled(true);
        mode = Player.Mode.Normal;
        myPlayer.ExitSittingMode();
        myPlayer.playerModeChanged.Trigger(mode);
        myPlayer.sitting = false;
        myPlayer.playerController.ForceControllerSize();
        myPlayer.transform.parent = null;
    }

    /// <summary>
    /// Executed by <see cref="DeselectSlots" />, as part of the player exiting helm control
    /// </summary>
    protected internal virtual void DoExitRoutines()
    {
        using var log = NewAvsLog();
        EndHelmControl(0.5f);
        MyExitLockedMode();
    }

    /// <summary>
    /// Checks if this vehicle can be piloted.
    /// </summary>
    public override bool CanPilot() => !FPSInputModule.current.lockMovement && IsPowered();


    /// <summary>
    /// True if the vehicle is currently boarded by the player.
    /// </summary>
    public bool IsBoarded
    {
        get => isBoarded;
        protected set => isBoarded = value;
        //IsPlayerDry = value;
    }

    /// <summary>
    /// Checks if the player is currently piloting this vehicle.
    /// </summary>
    public bool IsPlayerControlling() => PlayerAtHelm.IsNotNull();

    /// <summary>
    /// Executed has started being piloted by a player and <see cref="VehicleConfiguration.PilotingStyle" /> is set to <see cref="PilotingStyle.Other" />.
    /// </summary>
    /// <param name="isPiloting">True if the player is actually piloting</param>
    public virtual void HandleOtherPilotingAnimations(bool isPiloting)
    {
    }

    /// <summary>
    /// Checks if the player can exit helm control based on the current roll, pitch, and velocity.
    /// </summary>
    /// <param name="roll">Current roll delta angle from identity</param>
    /// <param name="pitch">Current pitch delta angle from identity</param>
    /// <param name="velocity">Current vehicle velocity</param>
    /// <returns>True if the player is permitted to exit helm control</returns>
    protected virtual bool PlayerCanExitHelmControl(float roll, float pitch, float velocity) => true;


    /// <summary>
    /// Invoked when <see cref="BeginHelmControl" /> starts.
    /// </summary>
    protected virtual void OnPreBeginHelmControl(Helm helm)
    {
    }

    /// <summary>
    /// Invoked when <see cref="BeginHelmControl" /> ends.
    /// </summary>
    protected virtual void OnBeginHelmControl(Helm helm)
    {
    }


    /// <summary>
    /// Invoked when <see cref="EndHelmControl" /> starts.
    /// </summary>
    protected virtual void OnPreEndHelmControl()
    {
    }

    /// <summary>
    /// Invoked when <see cref="EndHelmControl" /> ends.
    /// </summary>
    protected virtual void OnEndHelmControl()
    {
    }


    /// <summary>
    /// Invoked when <see cref="PlayerExit" /> starts.
    /// </summary>
    protected virtual void OnPrePlayerExit()
    {
    }

    /// <summary>
    /// Invoked when <see cref="PlayerExit" /> ends.
    /// </summary>
    protected virtual void OnPlayerExit()
    {
    }

    /// <summary>
    /// Invoked when <see cref="PlayerEntry" /> starts.
    /// </summary>
    protected virtual void OnPrePlayerEntry()
    {
    }

    /// <summary>
    /// Invoked when <see cref="PlayerEntry" /> ends.
    /// </summary>
    protected virtual void OnPlayerEntry()
    {
    }


    /// <summary>
    /// Queries the main helm of the vehicle.
    /// </summary>
    public abstract Helm GetMainHelm();

    /// <summary>
    /// Invoked when the player enters helm control.
    /// <see cref="Vehicle.playerPosition" /> is referenced by the base class implementation, so
    /// we do not forward the call when that field is null.
    /// </summary>
    /// <remarks>
    /// <see cref="Vehicle.playerPosition" /> is intentionally kept null while the player is not in control of the vehicle.
    /// Otherwise, clicking anywhere on the inside hull immediately causes the player to be teleported to
    /// and locked in the helm.
    /// </remarks>
    /// <param name="player">Player entering</param>
    /// <param name="teleport">If true, the player is localized to (0,0,0) after being reparented to the sub</param>
    /// <param name="playEnterAnimation">If true, the character enter animation is played</param>
    public sealed override void EnterVehicle(Player player, bool teleport, bool playEnterAnimation = true)
    {
        using var log = NewAvsLog();
        log.Write(
            $"{nameof(AvsVehicle)}.{nameof(EnterVehicle)} called with teleport={teleport}, playEnterAnimation={playEnterAnimation}");
        if (playerPosition.IsNotNull()) base.EnterVehicle(player, teleport, playEnterAnimation);
    }

    /// <summary>
    /// Begins helm control at the main helm of the vehicle.
    /// </summary>
    public void BeginMainHelmControl()
    {
        BeginHelmControl(GetMainHelm());
    }

    /// <summary>
    /// Enters the player into the sub, updates the quickbar and notifies the player of the piloting status.
    /// </summary>
    public void BeginHelmControl(
        Helm helm
    )
    {
        using var log = NewAvsLog();
        log.Debug(nameof(AvsVehicle) + '.' + nameof(BeginHelmControl));
        if (helm.PlayerControlLocation.IsNull())
            throw new NullReferenceException($"Helm must have a valid {nameof(helm.PlayerControlLocation)}");
        playerPosition = helm.PlayerControlLocation;
        if (energyInterface.IsNull())
            throw new NullReferenceException(
                $"{nameof(energyInterface)} must be set before calling {nameof(AvsVehicle)}.{nameof(BeginHelmControl)}");
        if (mainAnimator.IsNull())
            throw new NullReferenceException(
                $"{nameof(mainAnimator)} must be set before calling {nameof(AvsVehicle)}.{nameof(BeginHelmControl)}");
        try
        {
            OnPreBeginHelmControl(helm);
        }
        catch (Exception ex)
        {
            log.Error(nameof(OnPreBeginHelmControl), ex);
        }

        try
        {
            try
            {
                foreach (var window in Com.CanopyWindows) window.SafeSetActive(false);
            }
            catch (Exception)
            {
                //It's okay if the vehicle doesn't have a canopy
            }

            PlayerAtHelm = helm;
            playerSits = helm.IsSeated;
            log.Debug($"Player.playerController := {Player.main.playerController.NiceName()}");
            log.Debug($"Player.mode := {Player.main.mode}");
            log.Debug($"Vehicle.playerSits := {playerSits}");
            log.Debug($"Vehicle.playerPosition := {playerPosition.NiceName()}");
            log.Debug($"Vehicle.customGoalOnEnter := {customGoalOnEnter}");
            log.Debug($"Vehicle.energyInterface := {energyInterface.NiceName()}");
            log.Debug($"Vehicle.mainAnimator := {mainAnimator.NiceName()}");
            base.EnterVehicle(Player.main, true);
            uGUI.main.quickSlots.SetTarget(this);
            NotifyStatus(PlayerStatus.OnPilotBegin);
            if (helm.IsSeated)
                Character.SitDown(Owner);
            Character.SetArmsIKTargets(
                helm.LeftHandTarget,
                helm.RightHandTarget);

            try
            {
                OnBeginHelmControl(helm);
            }
            catch (Exception ex)
            {
                log.Error(nameof(OnBeginHelmControl), ex);
            }

            log.Debug(nameof(AvsVehicle) + '.' + nameof(BeginHelmControl) + " done");
        }
        catch (Exception ex)
        {
            log.Error(nameof(AvsVehicle) + '.' + nameof(BeginHelmControl), ex);
        }
    }

    /// <summary>
    /// Stops the piloting of the current vehicle and resets the control state.
    /// </summary>
    /// <remarks>This method disengages the player from controlling a vehicle and resets any
    /// associated UI elements.  It also triggers a notification to update the player's status to reflect the end of
    /// piloting.
    /// This is not the primary entry point to exit helm control, but rather a utility method.
    /// Call <see cref="ExitHelmControl"/> /<see cref="DeselectSlots" /> to exit helm control and reset the quickbar.
    /// </remarks>
    protected void EndHelmControl(float ikArmToggleTime)
    {
        using var log = NewAvsLog();
        try
        {
            OnPreEndHelmControl();
        }
        catch (Exception ex)
        {
            log.Error(nameof(OnPreEndHelmControl), ex);
        }

        try
        {
            if (playerSits)
            {
                Character.StandUp(Owner);
                playerSits = false;
            }

            if (IsBoarded)
                try
                {
                    foreach (var window in Com.CanopyWindows) window.SafeSetActive(true);
                }
                catch (Exception)
                {
                    //It's okay if the vehicle doesn't have a canopy
                }

            Character.SetArmsIKTargets(null, null, ikArmToggleTime);
            uGUI.main.quickSlots.SetTarget(null);
            NotifyStatus(PlayerStatus.OnPilotEnd);
            playerPosition = null;
            PlayerAtHelm = null;
            try
            {
                OnEndHelmControl();
            }
            catch (Exception ex)
            {
                log.Error(nameof(OnEndHelmControl), ex);
            }
        }
        catch (Exception ex)
        {
            log.Error(nameof(AvsVehicle) + '.' + nameof(EndHelmControl), ex);
        }
    }


    /// <summary>
    /// Finds the closest exit hatch to the player by comparing entry locations.
    /// </summary>
    /// <returns></returns>
    public VehicleHatchDefinition GetClosestExitHatch()
    {
        var idx = 0;
        var dist = (Com.Hatches[0].EntryLocation.position - Player.mainObject.transform.position).magnitude;
        for (var i = 1; i < Com.Hatches.Count; i++)
        {
            var newDist = (Com.Hatches[i].EntryLocation.position - Player.mainObject.transform.position).magnitude;
            if (newDist < dist)
            {
                dist = newDist;
                idx = i;
            }
        }

        return Com.Hatches[idx];
    }

    /// <summary>
    /// Finds the closest entry hatch to the player by comparing exit locations.
    /// </summary>
    public VehicleHatchDefinition GetClosestEntryHatch()
    {
        var idx = 0;
        var dist = (Com.Hatches[0].ExitLocation.position - Player.mainObject.transform.position).magnitude;
        for (var i = 1; i < Com.Hatches.Count; i++)
        {
            var newDist = (Com.Hatches[i].ExitLocation.position - Player.mainObject.transform.position).magnitude;
            if (newDist < dist)
            {
                dist = newDist;
                idx = i;
            }
        }

        return Com.Hatches[idx];
    }


    /// <summary>
    /// Enters the vehicle through the hatch closest to the player's current location.
    /// </summary>
    public void ClosestPlayerEntry()
    {
        PlayerEntry(GetClosestEntryHatch());
    }

    private bool SanitizePlayerForWalking(bool isBoarding)
    {
        using var log = NewAvsLog();
        var anyIssues = false;
        if (Player.main.currentMountedVehicle != this)
        {
            log.Write($"Updating currentMountedVehicle to this.");
            Player.main.currentMountedVehicle = this;
            anyIssues = true;
        }

        if (Player.main.sitting || Player.main.mode == Player.Mode.Sitting)
        {
            log.Write($"Player is sitting, exiting sitting mode.");
            Player.main.ExitSittingMode();
            Player.main.sitting = false;
            anyIssues = true;
        }

        if (Player.main.mode != Player.Mode.Normal)
        {
            log.Write($"Setting player mode to Normal.");
            Player.main.mode = Player.Mode.Normal;
            Player.main.playerModeChanged?.Trigger(Player.Mode.Normal);
            anyIssues = true;
        }

        if (Player.main.transform.parent != transform)
        {
            log.Write($"Player parent is not this vehicle, setting parent to this vehicle.");
            Player.main.transform.SetParent(transform);
            anyIssues = true;
        }

        if (Player.main.isUnderwater.value)
        {
            log.Write($"Player is underwater, setting to not.");
            Player.main.playerController.activeController.SetUnderWater(false);
            Player.main.playerController.SetEnabled(true);
            Player.main.SetScubaMaskActive(false);
            Player.main.isUnderwater.Update(false);
            anyIssues = true;
        }

        if (Player.main.isUnderwaterForSwimming.value)
        {
            log.Write($"Player is underwater for swimming, setting to not.");
            Player.main.isUnderwaterForSwimming.Update(false);
            anyIssues = true;
        }

        if (Player.main.motorMode != Player.MotorMode.Walk)
        {
            log.Write($"Player motor mode is not Walk, setting to Walk.");
            Player.main.playerController.SetMotorMode(Player.MotorMode.Walk);
            Player.main.motorMode = Player.MotorMode.Walk;
            Player.main.playerMotorModeChanged.Trigger(Player.MotorMode.Walk);
            anyIssues = true;
        }

        if (!AvatarInputHandler.main.gameObject.activeSelf)
        {
            log.Write($"AvatarInputHandler is not active, activating it.");
            AvatarInputHandler.main.gameObject.SetActive(true);
            anyIssues = true;
        }

        if (anyIssues)
        {
            Player.main.playerController.SetEnabled(true);
            Player.main.playerController.ForceControllerSize();
        }

        return anyIssues;
    }


    /// <summary>
    /// Internal registration for when the player enters the vehicle
    /// or was detected as entering the vehicle.
    /// </summary>
    /// <param name="ifNotDockedAction">Code to execute if not docked </param>
    /// <remarks>Designed to handle edge-cases like the player entering
    /// the tether space of the vehicle via teleport</remarks>
    internal void RegisterPlayerEntry(Action? ifNotDockedAction = null)
    {
        using var log = NewAvsLog();
        log.Debug(nameof(AvsVehicle) + '.' + nameof(RegisterPlayerEntry));
        if (!isScuttled /*&& !IsUnderCommand*/)
        {
            try
            {
                OnPrePlayerEntry();
            }
            catch (Exception ex)
            {
                log.Error(nameof(OnPrePlayerEntry), ex);
            }


            try
            {
                if (IsBoarded)
                    log.Warn(
                        $"{nameof(AvsVehicle)}.{nameof(RegisterPlayerEntry)}: Vehicle is already under command, re-executing registration.");
                IsBoarded = true;

                Player.main.lastValidSub = SubRoot;
                Player.main.SetCurrentSub(SubRoot, true);
                Player.main.SetScubaMaskActive(false);
                if (!IsVehicleDocked)
                {
                    ifNotDockedAction?.Invoke();
                    SanitizePlayerForWalking(true);
                    log.Write($"Player sanitized for walking");
                }
                else
                {
                    log.Write($"The vehicle registers as being docked. Some player updates cannot be performed");
                }

                NotifyStatus(PlayerStatus.OnPlayerEntry);
                HudPingInstance.enabled = false;

                try
                {
                    OnPlayerEntry();
                }
                catch (Exception ex)
                {
                    log.Error(nameof(OnPlayerEntry), ex);
                }

                log.Debug(nameof(AvsVehicle) + '.' + nameof(RegisterPlayerEntry) + " end");
            }
            catch (Exception ex)
            {
                log.Error(nameof(AvsVehicle) + '.' + nameof(RegisterPlayerEntry), ex);
            }
        }
        else
        {
            log.Error(
                $"{nameof(AvsVehicle)}.{nameof(RegisterPlayerEntry)}: Cannot register player entry, vehicle is scuttled ({isScuttled}) or already under command ({IsBoarded}).");
        }
    }

    /// <summary>
    /// Enters the player through the given hatch.
    /// </summary>
    public void PlayerEntry(VehicleHatchDefinition hatch)
    {
        using var log = NewAvsLog();
        log.Debug(nameof(AvsVehicle) + '.' + nameof(PlayerEntry));

        RegisterPlayerEntry(() =>
        {
            log.Debug($"Setting player to hatch entry location: {hatch.EntryLocation.position}");
            Player.main.transform.position = hatch.EntryLocation.position;
        });
        log.Debug(nameof(AvsVehicle) + '.' + nameof(PlayerEntry) + " done");
    }

    /// <summary>
    /// Exits the vehicle through the hatch closest to the player's current location.
    /// </summary>
    public void ClosestPlayerExit(bool canExitToSurface)
    {
        PlayerExit(GetClosestExitHatch(), canExitToSurface);
    }

    /// <summary>
    /// Called when the player exits the vehicle.
    /// </summary>
    public void PlayerExit(VehicleHatchDefinition hatch, bool canExitToSurface)
    {
        using var log = NewAvsLog();
        log.Debug($"{nameof(AvsVehicle)}.{nameof(PlayerExit)}");
        try
        {
            OnPrePlayerExit();
        }
        catch (Exception ex)
        {
            log.Error(nameof(OnPrePlayerExit), ex);
        }

        try
        {
            IsBoarded = false;
            if (Player.main.GetCurrentSub() == SubRoot) Player.main.SetCurrentSub(null);
            if (Player.main.GetVehicle() == this) Player.main.currentMountedVehicle = null;
            NotifyStatus(PlayerStatus.OnPlayerExit);
            Player.main.transform.SetParent(null);
            Player.main.TryEject(); // for DeathRun Remade Compat. See its patch in PlayerPatcher.cs
            HudPingInstance.enabled = true;
            if (!IsVehicleDocked)
            {
                if (transform.position.y < -3f || !canExitToSurface)
                {
                    if (hatch.ExitLocation.IsNull())
                    {
                        Logger.Error("Error: exitLocation is null. Cannot exit vehicle.");
                        return;
                    }

                    Player.main.transform.position = hatch.ExitLocation.position;
                }
                else
                {
                    Character.ExitToSurface(Owner, hatch.SurfaceExitLocation);
                }

                Player.main.transform.position = hatch.ExitLocation.position;
            }

            try
            {
                OnPlayerExit();
            }
            catch (Exception ex)
            {
                log.Error(nameof(OnPlayerExit), ex);
            }

            log.Debug($"{nameof(AvsVehicle)}.{nameof(PlayerExit)} end");
        }
        catch (Exception ex)
        {
            log.Error(nameof(AvsVehicle) + '.' + nameof(PlayerExit), ex);
        }
    }
}