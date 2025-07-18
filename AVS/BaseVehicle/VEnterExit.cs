using AVS.Configuration;
using AVS.Util;
using AVS.VehicleParts;
using System;
using UnityEngine;

namespace AVS.BaseVehicle
{
    public abstract partial class AvsVehicle
    {
        private int enteredThroughHatch = 0;

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
        /// Checks if the vehicle is currently under the command of the player.
        /// </summary>
        public bool IsUnderCommand
        {// true when inside a vehicle (or piloting a drone)
            get
            {
                return isUnderCommand;
            }
            protected set
            {
                isUnderCommand = value;
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
                return sub2.IsUnderCommand;
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
        /// piloting.</remarks>
        public void EndHelmControl(float ikArmToggleTime)
        {
            if (Player.main.currentSub != null && Player.main.currentSub.name.ToLower().Contains("cyclops"))
            {
                //Unfortunately, this method shares a name with some Cyclops components.
                // PilotingChair.ReleaseBy broadcasts a message for "StopPiloting"
                // So because a docked vehicle is part of the Cyclops heirarchy,
                // it tries to respond, which causes a game crash.
                // So we'll return if the player is within a Cyclops.
                return;
            }
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


        /// <summary>
        /// Internal registration for when the player enters the vehicle
        /// or was detected as entering the vehicle.
        /// </summary>
        /// <param name="dockedCallback">Code to execute if not docked </param>
        /// <remarks>Designed to handle edge-cases like the player entering
        /// the tether space of the vehicle via teleport</remarks>
        internal void RegisterPlayerEntry(Action? dockedCallback = null)
        {
            Log.Debug(this, nameof(AvsVehicle) + '.' + nameof(RegisterPlayerEntry));
            if (!isScuttled && !IsUnderCommand)
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

                    IsUnderCommand = true;
                    Player.main.SetScubaMaskActive(false);
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
                    Player.main.lastValidSub = SubRoot;
                    Player.main.SetCurrentSub(SubRoot, true);
                    Player.main.SetScubaMaskActive(false);
                    if (!IsVehicleDocked)
                    {
                        Player.main.currentMountedVehicle = this;
                        Player.main.transform.SetParent(transform);
                        dockedCallback?.Invoke();
                        Player.main.playerController.activeController.SetUnderWater(false);
                        Player.main.isUnderwater.Update(false);
                        Player.main.isUnderwaterForSwimming.Update(false);
                        Player.main.playerController.SetMotorMode(Player.MotorMode.Walk);
                        Player.main.motorMode = Player.MotorMode.Walk;
                        Player.main.playerMotorModeChanged.Trigger(Player.MotorMode.Walk);
                    }

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
                if (IsUnderCommand)
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
                IsUnderCommand = false;
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
