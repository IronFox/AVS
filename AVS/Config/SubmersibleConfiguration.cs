using System;
using System.Collections.Generic;

namespace AVS.Config
{
    public class SubmersibleConfiguration : VehicleConfiguration
    {
        //set  PilotingStyle to Seamoth
        //must be set
        public VehicleParts.VehiclePilotSeat PilotSeat { get; }
        //must be non-empty
        public IReadOnlyList<VehicleParts.VehicleHatchStruct> Hatches { get; } = Array.Empty<VehicleParts.VehicleHatchStruct>();

    }
}
