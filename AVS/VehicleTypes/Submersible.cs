using AVS.BaseVehicle;
using AVS.Composition;
using AVS.Configuration;
using AVS.VehicleParts;
using System;

namespace AVS.VehicleTypes
{
    /*
     * Submersible is the class of non-walkable vehicles
     */
    public abstract class Submersible : AvsVehicle
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
            EndHelmControl(0.5f);
        }
        public override bool CanPilot()
        {
            return !FPSInputModule.current.lockMovement && IsPowered();
        }


        /// <inheritdoc/>
        public override Helm GetMainHelm()
            => Com.PilotSeat;

        /// <summary>
        /// Begins piloting the submersible from the given seat.
        /// </summary>
        public void EnterHelmControl()
        {
            BeginHelmControl(Com.PilotSeat);
        }
        /// <inheritdoc/>
        protected override void OnEndHelmControl()
        {
            ClosestPlayerExit(true);
        }
        /// <inheritdoc/>
        protected override void OnPlayerEntry()
        {
            if (!isScuttled)
            {
                Log.Debug(this, "start submersible player entry");
                Player.main.currentSub = null;
                Player.main.transform.SetParent(transform);
                EnterHelmControl();
            }
        }
        //public override void PlayerExit()
        //{
        //    base.PlayerExit();
        //    Log.Debug(this, "start submersible player exit");
        //    if (!IsVehicleDocked)
        //    {
        //        Player.main.transform.SetParent(null);
        //        Player.main.transform.position = Com.Hatches.First().ExitLocation.position;
        //    }
        //    else
        //    {
        //        UWE.CoroutineHost.StartCoroutine(EventuallyStandUp());
        //    }
        //    Log.Debug(this, "end submersible player exit");
        //}
    }
}
