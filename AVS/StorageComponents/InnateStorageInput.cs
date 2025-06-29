﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AVS
{
    public class InnateStorageInput : StorageInput
    {
		public override void OpenFromExternal()
		{
			ItemsContainer storageInSlot = mv.ModGetStorageInSlot(slotID, VehicleBuilder.InnateStorage);
			if (storageInSlot != null)
			{
				PDA pda = Player.main.GetPDA();
				Inventory.main.SetUsedStorage(storageInSlot, false);
				pda.Open(PDATab.Inventory, null, null);
			}
		}
		protected override void OpenPDA()
		{
			ItemsContainer storageInSlot = mv.ModGetStorageInSlot(slotID, VehicleBuilder.InnateStorage);
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
