namespace AVS.UpgradeModules
{
    public abstract class SelectableUpgrade : AvsVehicleUpgrade
    {
        public override string Description => "This is a selectable upgrade module.";
        public override QuickSlotType QuickSlotType => QuickSlotType.Selectable;
        public virtual float Cooldown => 0;
        public virtual float EnergyCost => 0;
        public virtual void OnSelected(SelectableActionParams param)
        {
            Logger.DebugLog(this, "Selecting " + ClassId + " on ModVehicle: " + param.Vehicle.subName.name + " in slotID: " + param.SlotID.ToString());
        }
    }
}
