﻿namespace AVS.UpgradeTypes
{
    public abstract class SelectableChargeableUpgrade : AvsVehicleUpgrade
    {
        public override string Description => "This is a selectable-chargeable upgrade module.";
        public override QuickSlotType QuickSlotType => QuickSlotType.SelectableChargeable;
        public virtual float MaxCharge => 0;
        public virtual float EnergyCost => 0;
        public virtual void OnSelected(SelectableChargeableActionParams param)
        {
            Logger.DebugLog(this, "Selecting-Charging " + ClassId + " on ModVehicle: " + param.Vehicle.subName.name + " in slotID: " + param.SlotID.ToString());
        }
    }
}
