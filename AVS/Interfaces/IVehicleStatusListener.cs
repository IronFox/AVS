﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVS
{
    public enum VehicleStatus
    {
        OnTakeDamage,
        OnNearbyLeviathan
    }
    public interface IVehicleStatusListener
    {
        void OnTakeDamage();
        void OnNearbyLeviathan();
    }
}
