using AVS.Composition;
using AVS.Configuration;
using AVS.Util;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace AVS.VehicleTypes
{
    /*
     * Submersible is the class of non-walkable vehicles
     */
    public abstract class Submersible : ModVehicle
    {
        public Submersible(VehicleConfiguration config) : base(config)
        { }

        public abstract SubmersibleComposition GetSubmersibleComposition();
        public sealed override VehicleComposition GetVehicleComposition()
        {
            _subComposition = GetSubmersibleComposition();
            return _subComposition;
        }

        private SubmersibleComposition? _subComposition;
        public new SubmersibleComposition Com =>
            _subComposition
            ?? throw new InvalidOperationException("This vehicle's composition has not yet been initialized. Please wait until Submersible.Awake() has been called");

        /// <inheritdoc/>
        protected internal override void DoExitRoutines()
        {
            Player myPlayer = Player.main;
            Player.Mode myMode = myPlayer.mode;

            DoCommonExitActions(ref myMode);
            myPlayer.mode = myMode;
            StopPiloting();
        }
        public override bool CanPilot()
        {
            return !FPSInputModule.current.lockMovement && IsPowered();
        }
        protected IEnumerator SitDownInChair()
        {
            Player.main.playerAnimator.SetBool("chair_sit", true);
            yield return null;
            Player.main.playerAnimator.SetBool("chair_sit", false);
            Player.main.playerAnimator.speed = 100f;
            yield return new WaitForSeconds(0.05f);
            Player.main.playerAnimator.speed = 1f;
        }
        protected IEnumerator StandUpFromChair()
        {
            Player.main.playerAnimator.SetBool("chair_stand_up", true);
            yield return null;
            Player.main.playerAnimator.SetBool("chair_stand_up", false);
            Player.main.playerAnimator.speed = 100f;
            yield return new WaitForSeconds(0.05f);
            Player.main.playerAnimator.speed = 1f;
            yield return null;
        }
        public IEnumerator EventuallyStandUp()
        {
            yield return new WaitForSeconds(1f);
            yield return StandUpFromChair();
            uGUI.main.quickSlots.SetTarget(null);
            Player.main.currentMountedVehicle = null;
            Player.main.transform.SetParent(null);
        }
        public override void BeginPiloting()
        {
            base.BeginPiloting();
            Player.main.EnterSittingMode();
            UWE.CoroutineHost.StartCoroutine(SitDownInChair());
            //StartCoroutine(TryStandUpFromChair());
            Player.main.armsController.ikToggleTime = 0;
            Player.main.armsController.SetWorldIKTarget(Com.SteeringWheelLeftHandTarget.GetTransform(), Com.SteeringWheelRightHandTarget.GetTransform());
        }
        public override void StopPiloting()
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
            UWE.CoroutineHost.StartCoroutine(StandUpFromChair());
            Player.main.armsController.ikToggleTime = 0.5f;
            Player.main.armsController.SetWorldIKTarget(null, null);
            uGUI.main.quickSlots.SetTarget(null);
            PlayerExit();
        }
        public override void PlayerEntry()
        {
            base.PlayerEntry();
            if (!isScuttled)
            {
                Log.Debug(this, "start submersible player entry");
                Player.main.currentSub = null;
                Player.main.currentMountedVehicle = this;
                Player.main.transform.SetParent(transform);
                //Player.main.playerController.activeController.SetUnderWater(false);
                //Player.main.isUnderwater.Update(false);
                //Player.main.isUnderwaterForSwimming.Update(false);
                //Player.main.playerController.SetMotorMode(Player.MotorMode.Walk);
                //Player.main.motorMode = Player.MotorMode.Walk;
                Player.main.SetScubaMaskActive(false);
                //Player.main.playerMotorModeChanged.Trigger(Player.MotorMode.Walk);
                BeginPiloting();
            }
        }
        public override void PlayerExit()
        {
            base.PlayerExit();
            Log.Debug(this, "start submersible player exit");
            if (!IsVehicleDocked)
            {
                Player.main.transform.SetParent(null);
                Player.main.transform.position = Com.Hatches.First().ExitLocation.position;
            }
            else
            {
                UWE.CoroutineHost.StartCoroutine(EventuallyStandUp());
            }
            Log.Debug(this, "end submersible player exit");
        }
    }
}
