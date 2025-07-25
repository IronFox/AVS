﻿namespace AVS.UpgradeTypes
{
    public abstract class ToggleableUpgrade : AvsVehicleUpgrade
    {
        public override string Description => "This is a toggleable upgrade module.";
        public override QuickSlotType QuickSlotType => QuickSlotType.Toggleable;
        public virtual float RepeatRate => 0;
        public virtual float TimeToFirstActivation => 0;
        public virtual float EnergyCostPerActivation => 0;
        public virtual void OnRepeat(ToggleActionParams param)
        {
            Logger.DebugLog(this, "Selecting " + ClassId + " on ModVehicle: " + param.vehicle.subName.name + " in slotID: " + param.slotID.ToString());
        }
    }
}
