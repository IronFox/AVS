using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVS.VehicleComponents
{
    
    /// <summary>
    /// Energy mixin for a power cell that never depletes
    /// </summary>
    internal class ForeverBattery : EnergyMixin
    {
        // use fixed update because EnergyMixin has none
        public void FixedUpdate()
        {
            AddEnergy(capacity - charge);
        }
    }
}
