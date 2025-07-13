using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVS.Saving
{
    /// <summary>
    /// Save data for submarines.
    /// </summary>
    public class SubmarineSaveData : VehicleSaveData
    {
        /// <summary>
        /// True if the default coloring should be used for the name display
        /// </summary>
        public bool DefaultColorName { get; set; } = false;
        /// <summary>
        /// The seat currently used by the player to pilot this submarine.
        /// </summary>
        public int CurrentPilotSeatIndex { get; set; }
    }
}
