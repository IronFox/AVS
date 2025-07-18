using AVS.BaseVehicle;
using AVS.Composition;
using AVS.Configuration;
using System;

namespace AVS.VehicleTypes
{
    public abstract class Skimmer : AvsVehicle
    {
        public Skimmer(VehicleConfiguration config) : base(config)
        { }
        public abstract SkimmerComposition GetSkimmerComposition();
        public sealed override VehicleComposition GetVehicleComposition()
        {
            _skimmerConfig = GetSkimmerComposition();
            return _skimmerConfig;
        }

        private SkimmerComposition? _skimmerConfig;
        public new SkimmerComposition Com =>
            _skimmerConfig
            ?? throw new InvalidOperationException("This vehicle's composition has not yet been initialized. Please wait until Skimmer.Awake() has been called");


        protected bool isPlayerInside = false;

        public bool IsPlayerInside()
        {
            // this one is correct ?
            return isPlayerInside;
        }
        /// <inheritdoc/>
        protected internal override void DoExitRoutines()
        {
            Player myPlayer = Player.main;
            Player.Mode myMode = myPlayer.mode;

            DoCommonExitActions(ref myMode);
            myPlayer.mode = myMode;
            EndHelmControl(0.5f);
        }
    }
}
