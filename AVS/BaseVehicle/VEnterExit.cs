using AVS.Configuration;
using AVS.Util;
using AVS.VehicleParts;
using System;
using UnityEngine;

namespace AVS.BaseVehicle
{
    public abstract partial class AvsVehicle
    {
        /// <summary>
        /// True if the player is currently piloting the vehicle.
        /// </summary>
        public bool IsHelmControlling { get; private set; } = false;

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
            Player myPlayer = Player.main;
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
        internal protected virtual void DoExitRoutines()
        {
            Log.Debug(this, nameof(DoExitRoutines));
            MyExitLockedMode();
        }


        /// <summary>
        /// True if the vehicle is currently boarded by the player.
        /// </summary>
        public bool IsBoarded
        {
            get
            {
                return isBoarded;
            }
            protected set
            {
                isBoarded = value;
                //IsPlayerDry = value;
            }
        }

        /// <summary>
        /// Checks if the player is currently piloting this vehicle.
        /// </summary>
        public virtual bool IsPlayerControlling()
        {
            if (this is VehicleTypes.Submarine sub)
            {
                return sub.IsPlayerPiloting();
            }
            else if (this is AvsVehicle sub2)
            {
                return sub2.IsHelmControlling;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Executed has started being piloted by a player and <see cref="VehicleConfiguration.PilotingStyle" /> is set to <see cref="PilotingStyle.Other" />.
        /// </summary>
        /// <param name="isPiloting">True if the player is actually piloting</param>
        public virtual void HandleOtherPilotingAnimations(bool isPiloting) { }

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
        { }
        /// <summary>
        /// Invoked when <see cref="BeginHelmControl" /> ends.
        /// </summary>
        protected virtual void OnBeginHelmControl(Helm helm)
        { }


        /// <summary>
        /// Invoked when <see cref="EndHelmControl" /> starts.
        /// </summary>
        protected virtual void OnPreEndHelmControl()
        { }
        /// <summary>
        /// Invoked when <see cref="EndHelmControl" /> ends.
        /// </summary>
        protected virtual void OnEndHelmControl()
        { }


        /// <summary>
        /// Invoked when <see cref="PlayerExit" /> starts.
        /// </summary>
        protected virtual void OnPrePlayerExit()
        { }
        /// <summary>
        /// Invoked when <see cref="PlayerExit" /> ends.
        /// </summary>
        protected virtual void OnPlayerExit()
        { }

        /// <summary>
        /// Invoked when <see cref="PlayerEntry" /> starts.
        /// </summary>
        protected virtual void OnPrePlayerEntry()
        { }

        /// <summary>
        /// Invoked when <see cref="PlayerEntry" /> ends.
        /// </summary>
        protected virtual void OnPlayerEntry()
        { }


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
            Log.Write($"{nameof(AvsVehicle)}.{nameof(EnterVehicle)} called with teleport={teleport}, playEnterAnimation={playEnterAnimation}");
            if (playerPosition != null)
            {
                base.EnterVehicle(player, teleport, playEnterAnimation);
            }
        }

        /// <summary>
        /// Enters the player into the sub, updates the quickbar and notifies the player of the piloting status.
        /// </summary>
        public void BeginHelmControl(
            Helm helm
            )
        {
            Log.Debug(this, nameof(AvsVehicle) + '.' + nameof(BeginHelmControl));
            if (helm.PlayerControlLocation == null)
                throw new NullReferenceException($"Helm must have a valid {nameof(helm.PlayerControlLocation)}");
            playerPosition = helm.PlayerControlLocation;
            if (energyInterface == null)
                throw new NullReferenceException($"{nameof(energyInterface)} must be set before calling {nameof(AvsVehicle)}.{nameof(BeginHelmControl)}");
            if (mainAnimator == null)
                throw new NullReferenceException($"{nameof(mainAnimator)} must be set before calling {nameof(AvsVehicle)}.{nameof(BeginHelmControl)}");
            try
            {
                OnPreBeginHelmControl(helm);
            }
            catch (Exception ex)
            {
                Log.Error(nameof(OnPreBeginHelmControl), ex);
            }
            try
            {
                try
                {
                    foreach (GameObject window in Com.CanopyWindows)
                    {
                        window.SafeSetActive(false);
                    }
                }
                catch (Exception)
                {
                    //It's okay if the vehicle doesn't have a canopy
                }
                IsHelmControlling = true;
                playerSits = helm.IsSeated;
                Log.Debug(this, $"Player.playerController := {Player.main.playerController.NiceName()}");
                Log.Debug(this, $"Player.mode := {Player.main.mode}");
                Log.Debug(this, $"Vehicle.playerSits := {playerSits}");
                Log.Debug(this, $"Vehicle.playerPosition := {playerPosition.NiceName()}");
                Log.Debug(this, $"Vehicle.customGoalOnEnter := {customGoalOnEnter}");
                Log.Debug(this, $"Vehicle.energyInterface := {energyInterface.NiceName()}");
                Log.Debug(this, $"Vehicle.mainAnimator := {mainAnimator.NiceName()}");
                base.EnterVehicle(Player.main, true);
                uGUI.main.quickSlots.SetTarget(this);
                NotifyStatus(PlayerStatus.OnPilotBegin);
                if (helm.IsSeated)
                    Character.SitDown();
                Character.SetArmsIKTargets(
                    leftHandTarget: helm.LeftHandTarget,
                    rightHandTarget: helm.RightHandTarget);

                try
                {
                    OnBeginHelmControl(helm);
                }
                catch (Exception ex)
                {
                    Log.Error(nameof(OnBeginHelmControl), ex);
                }

                Log.Debug(this, nameof(AvsVehicle) + '.' + nameof(BeginHelmControl) + " done");
            }
            catch (Exception ex)
            {
                Log.Error(nameof(AvsVehicle) + '.' + nameof(BeginHelmControl), ex);
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
            try
            {
                OnPreEndHelmControl();
            }
            catch (Exception ex)
            {
                Log.Error(nameof(OnPreEndHelmControl), ex);
            }
            try
            {
                if (playerSits)
                {
                    Character.StandUp();
                    playerSits = false;
                }

                if (IsBoarded)
                {
                    try
                    {
                        foreach (GameObject window in Com.CanopyWindows)
                        {
                            window.SafeSetActive(true);
                        }
                    }
                    catch (Exception)
                    {
                        //It's okay if the vehicle doesn't have a canopy
                    }
                }

                Character.SetArmsIKTargets(leftHandTarget: null, rightHandTarget: null, ikArmToggleTime);
                uGUI.main.quickSlots.SetTarget(null);
                NotifyStatus(PlayerStatus.OnPilotEnd);
                playerPosition = null;
                try
                {
                    OnEndHelmControl();
                }
                catch (Exception ex)
                {
                    Log.Error(nameof(OnEndHelmControl), ex);
                }
            }
            catch (Exception ex)
            {
                Log.Error(nameof(AvsVehicle) + '.' + nameof(EndHelmControl), ex);
            }
        }


        /// <summary>
        /// Finds the closest exit hatch to the player by comparing entry locations.
        /// </summary>
        /// <returns></returns>
        public VehicleHatchDefinition GetClosestExitHatch()
        {
            int idx = 0;
            float dist = (Com.Hatches[0].EntryLocation.position - Player.mainObject.transform.position).magnitude;
            for (int i = 1; i < Com.Hatches.Count; i++)
            {
                float newDist = (Com.Hatches[i].EntryLocation.position - Player.mainObject.transform.position).magnitude;
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
            int idx = 0;
            float dist = (Com.Hatches[0].ExitLocation.position - Player.mainObject.transform.position).magnitude;
            for (int i = 1; i < Com.Hatches.Count; i++)
            {
                float newDist = (Com.Hatches[i].ExitLocation.position - Player.mainObject.transform.position).magnitude;
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
            bool anyIssues = false;
            if (Player.main.currentMountedVehicle != this)
            {
                Log.Write($"{nameof(SanitizePlayerForWalking)}: Updating currentMountedVehicle to this.");
                Player.main.currentMountedVehicle = this;
                anyIssues = true;
            }
            if (Player.main.sitting || Player.main.mode == Player.Mode.Sitting)
            {
                Log.Write($"{nameof(SanitizePlayerForWalking)}: Player is sitting, exiting sitting mode.");
                Player.main.ExitSittingMode();
                Player.main.sitting = false;
                anyIssues = true;
            }
            if (Player.main.mode != Player.Mode.Normal)
            {
                Log.Write($"{nameof(SanitizePlayerForWalking)}: Setting player mode to Normal.");
                Player.main.mode = Player.Mode.Normal;
                Player.main.playerModeChanged?.Trigger(Player.Mode.Normal);
                anyIssues = true;
            }
            if (Player.main.transform.parent != transform)
            {
                Log.Write($"{nameof(SanitizePlayerForWalking)}: Player parent is not this vehicle, setting parent to this vehicle.");
                Player.main.transform.SetParent(transform);
                anyIssues = true;
            }
            if (Player.main.isUnderwater.value)
            {
                Log.Write($"{nameof(SanitizePlayerForWalking)}: Player is underwater, setting to not.");
                Player.main.playerController.activeController.SetUnderWater(false);
                Player.main.playerController.SetEnabled(enabled: true);
                Player.main.SetScubaMaskActive(false);
                Player.main.isUnderwater.Update(false);
                anyIssues = true;
            }
            if (Player.main.isUnderwaterForSwimming.value)
            {
                Log.Write($"{nameof(SanitizePlayerForWalking)}: Player is underwater for swimming, setting to not.");
                Player.main.isUnderwaterForSwimming.Update(false);
                anyIssues = true;
            }
            if (Player.main.motorMode != Player.MotorMode.Walk)
            {
                Log.Write($"{nameof(SanitizePlayerForWalking)}: Player motor mode is not Walk, setting to Walk.");
                Player.main.playerController.SetMotorMode(Player.MotorMode.Walk);
                Player.main.motorMode = Player.MotorMode.Walk;
                Player.main.playerMotorModeChanged.Trigger(Player.MotorMode.Walk);
                anyIssues = true;
            }
            if (!AvatarInputHandler.main.gameObject.activeSelf)
            {
                Log.Write($"{nameof(SanitizePlayerForWalking)}: AvatarInputHandler is not active, activating it.");
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
            Log.Debug(this, nameof(AvsVehicle) + '.' + nameof(RegisterPlayerEntry));
            if (!isScuttled /*&& !IsUnderCommand*/)
            {
                try
                {
                    OnPrePlayerEntry();
                }
                catch (Exception ex)
                {
                    Log.Error(nameof(OnPrePlayerEntry), ex);
                }


                try
                {
                    if (IsBoarded)
                    {
                        Log.Warn($"{nameof(AvsVehicle)}.{nameof(RegisterPlayerEntry)}: Vehicle is already under command, re-executing registration.");
                    }
                    IsBoarded = true;

                    Player.main.lastValidSub = SubRoot;
                    Player.main.SetCurrentSub(SubRoot, true);
                    Player.main.SetScubaMaskActive(false);
                    if (!IsVehicleDocked)
                    {
                        ifNotDockedAction?.Invoke();
                        SanitizePlayerForWalking(true);
                        Log.Write($"Player sanitized for walking");

                    }
                    else
                        Log.Write($"The vehicle registers as being docked. Some player updates cannot be performed");

                    NotifyStatus(PlayerStatus.OnPlayerEntry);
                    HudPingInstance.enabled = false;

                    try
                    {
                        OnPlayerEntry();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(nameof(OnPlayerEntry), ex);
                    }
                    Log.Debug(this, nameof(AvsVehicle) + '.' + nameof(RegisterPlayerEntry) + " end");

                }
                catch (Exception ex)
                {
                    Log.Error(nameof(AvsVehicle) + '.' + nameof(RegisterPlayerEntry), ex);
                }
            }
            else
                Log.Error($"{nameof(AvsVehicle)}.{nameof(RegisterPlayerEntry)}: Cannot register player entry, vehicle is scuttled ({isScuttled}) or already under command ({IsBoarded}).");
        }

        /// <summary>
        /// Enters the player through the given hatch.
        /// </summary>
        public void PlayerEntry(VehicleHatchDefinition hatch)
        {
            Log.Debug(this, nameof(AvsVehicle) + '.' + nameof(PlayerEntry));

            RegisterPlayerEntry(() =>
            {
                Log.Debug(this, $"Setting player to hatch entry location: {hatch.EntryLocation.position}");
                Player.main.transform.position = hatch.EntryLocation.position;
            });
            Log.Debug(this, nameof(AvsVehicle) + '.' + nameof(PlayerEntry) + " done");
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
            Log.Debug(this, $"{nameof(AvsVehicle)}.{nameof(PlayerExit)}");
            try
            {
                OnPrePlayerExit();
            }
            catch (Exception ex)
            {
                Log.Error(nameof(OnPrePlayerExit), ex);
            }
            try
            {
                IsBoarded = false;
                if (Player.main.GetCurrentSub() == SubRoot)
                {
                    Player.main.SetCurrentSub(null);
                }
                if (Player.main.GetVehicle() == this)
                {
                    Player.main.currentMountedVehicle = null;
                }
                NotifyStatus(PlayerStatus.OnPlayerExit);
                Player.main.transform.SetParent(null);
                Player.main.TryEject(); // for DeathRun Remade Compat. See its patch in PlayerPatcher.cs
                HudPingInstance.enabled = true;
                if (!IsVehicleDocked)
                {
                    if (transform.position.y < -3f || !canExitToSurface)
                    {
                        if (hatch.ExitLocation == null)
                        {
                            Logger.Error("Error: exitLocation is null. Cannot exit vehicle.");
                            return;
                        }
                        Player.main.transform.position = hatch.ExitLocation.position;
                    }
                    else
                    {
                        Character.ExitToSurface(hatch.SurfaceExitLocation);
                    }
                    Player.main.transform.position = hatch.ExitLocation.position;
                }
                try
                {
                    OnPlayerExit();
                }
                catch (Exception ex)
                {
                    Log.Error(nameof(OnPlayerExit), ex);
                }
                Log.Debug(this, $"{nameof(AvsVehicle)}.{nameof(PlayerExit)} end");
            }
            catch (Exception ex)
            {
                Log.Error(nameof(AvsVehicle) + '.' + nameof(PlayerExit), ex);
            }
        }
    }
}
