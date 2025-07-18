using AVS.BaseVehicle;
using System.Collections.Generic;
using System.Linq;

namespace AVS
{
    public class ModularStorageInput : StorageInput
    {
        public override void OpenFromExternal()
        {
            if (mv == null)
            {
                return;
            }
            var storageInSlot = mv.ModGetStorageInSlot(slotID, TechType.VehicleStorageModule);
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
                return;
            }
            var storageInSlot = mv.ModGetStorageInSlot(slotID, TechType.VehicleStorageModule);
            if (storageInSlot == null)
            {
                storageInSlot = gameObject.GetComponent<SeamothStorageContainer>().container;
            }

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
        public static List<ItemsContainer> GetAllModularStorageContainers(AvsVehicle mv)
        {
            List<ItemsContainer> result = new List<ItemsContainer>();
            if (mv == null)
            {
                return result;
            }
            var containerList = mv.Com.ModulesRootObject.GetComponentsInChildren<SeamothStorageContainer>(true);
            if (!containerList.Any())
            {
                return result;
            }
            return containerList.Select(x => x.container).ToList();
        }
    }
}
