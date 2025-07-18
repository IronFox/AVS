﻿using AVS.BaseVehicle;
using System;

namespace AVS.VehicleComponents
{
    public abstract class AvsVehicleDockingBay : DockingBay
    {
        private AvsVehicle mv => GetComponent<AvsVehicle>();
        protected float rechargePerFrame = 1f;
        protected override void OnFinishedDocking(Vehicle dockingVehicle)
        {
            base.OnFinishedDocking(dockingVehicle);
            if (Player.main.currentMountedVehicle == dockingVehicle)
            {
                mv.ClosestPlayerEntry();
            }
        }
        protected override void OnStartedUndocking(bool withPlayer)
        {
            if (withPlayer)
            {
                mv.ClosestPlayerExit(false);
            }
            base.OnStartedUndocking(withPlayer);
        }
        protected override void TryRechargeDockedVehicle()
        {
            if (currentDockedVehicle == null)
            {
                return;
            }
            base.TryRechargeDockedVehicle();
            mv.GetEnergyValues(out float charge, out float _);
            currentDockedVehicle.GetEnergyValues(out float dockedEnergy, out float dockedCapacity);
            float dockDesires = dockedCapacity - dockedEnergy;
            float dockRecharge = Math.Min(1, dockDesires);
            if (charge > dockRecharge && dockRecharge > 0)
            {
                float actual = mv.PowerManager.TrySpendEnergy(dockRecharge);
                currentDockedVehicle.AddEnergy(actual);
            }
        }
    }
}
