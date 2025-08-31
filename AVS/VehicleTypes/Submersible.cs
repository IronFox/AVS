using AVS.BaseVehicle;
using AVS.Composition;
using AVS.Configuration;
using AVS.VehicleBuilding;
using System;

namespace AVS.VehicleTypes
{
    /// <summary>
    /// Non-walkable vehicle type that can be piloted underwater.
    /// </summary>
    public abstract class Submersible : AvsVehicle
    {
        /// <summary>
        /// Constructs the vehicle with the given configuration.
        /// </summary>
        /// <param name="config">Vehicle configuration. Must not be null</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected Submersible(VehicleConfiguration config) : base(config)
        { }

        /// <summary>
        /// Retrieves the composition of the submarine.
        /// Executed once either during <see cref="AvsVehicle.Awake()"/> or vehicle registration, whichever comes first.
        /// </summary>
        protected abstract SubmersibleComposition GetSubmersibleComposition();
        /// <inheritdoc/>
        protected sealed override VehicleComposition GetVehicleComposition()
        {
            _subComposition = GetSubmersibleComposition();
            return _subComposition;
        }

        private SubmersibleComposition? _subComposition;
        /// <summary>
        /// Resolved vehicle composition.
        /// If accessed before <see cref="AvsVehicle.Awake()"/> (or vehicle registration), InvalidOperationException will be thrown.
        /// </summary>
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
            using var log = NewAvsLog();
            if (!isScuttled)
            {
                log.Debug("start submersible player entry");
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
        //        MainPatcher.Instance.StartCoroutine(EventuallyStandUp());
        //    }
        //    Log.Debug(this, "end submersible player exit");
        //}
    }
}
