using AVS.Log;

namespace AVS.StorageComponents
{
    internal class WaterParkStorageInput : StorageInput
    {
        public override void OpenFromExternal()
        {
            if (mv == null)
            {
                LogWriter.Default.Error("AvsVehicle is null in WaterParkStorageInput.OpenFromExternal");
                return;
            }
            if (slotID >= mv.Com.WaterParks.Count)
            {
                LogWriter.Default.Error($"Invalid slotID {slotID} in WaterParkStorageInput.OpenFromExternal");
                return;
            }
            var mwp = mv.Com.WaterParks[slotID].Container.GetComponent<MobileWaterPark>();
            if (mwp == null)
            {
                mv.Log.Error("MobileWaterPark is null in WaterParkStorageInput.OpenFromExternal");
                return;
            }
            var storageInSlot = mwp.Container;
            if (storageInSlot != null)
            {
                PDA pda = Player.main.GetPDA();
                Inventory.main.SetUsedStorage(storageInSlot, false);
                pda.Open(PDATab.Inventory, null, null);
            }
        }
        protected override void OpenPDA()
        {
            if (mv == null)
            {
                LogWriter.Default.Error("AvsVehicle is null in WaterParkStorageInput.OpenPDA");
                return;
            }
            if (slotID >= mv.Com.WaterParks.Count)
            {
                LogWriter.Default.Error($"Invalid slotID {slotID} in WaterParkStorageInput.OpenFromExternal");
                return;
            }
            var mwp = mv.Com.WaterParks[slotID].Container.GetComponent<MobileWaterPark>();
            if (mwp == null)
            {
                mv.Log.Error("MobileWaterPark is null in WaterParkStorageInput.OpenFromExternal");
                return;
            }

            var storageInSlot = mwp.Container;
            if (storageInSlot != null)
            {
                PDA pda = Player.main.GetPDA();
                Inventory.main.SetUsedStorage(storageInSlot, false);
                if (!pda.Open(PDATab.Inventory, this.tr, new PDA.OnClose(this.OnClosePDA)))
                {
                    this.OnClosePDA(pda);
                    return;
                }
            }
            else
            {
                this.OnClosePDA(null);
            }
        }
    }
}
