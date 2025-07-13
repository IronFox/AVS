using AVS.VehicleTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AVS.Saving
{
    /// <summary>
    /// Basic save data written for all vehicles
    /// </summary>
    public class VehicleSaveData
    {
        /// <summary>
        /// The player is currently at the helm of this vehicle.
        /// </summary>
        public bool IsControlling { get; set; }

        /// <summary>
        /// The player is currently inside this vehicle
        /// </summary>
        public bool IsInside { get; set; }
        /// <summary>
        /// The given name of this vehicle.
        /// </summary>
        public string VehicleName { get; set; } = "";
        /// <summary>
        /// The set base color of this vehicle
        /// </summary>
        public SavedColor? BaseColor { get; set; }
        /// <summary>
        /// The set interior color of this vehicle
        /// </summary>
        public SavedColor? InteriorColor { get; set; }
        /// <summary>
        /// The set stripe color of this vehicle
        /// </summary>
        public SavedColor? StripeColor { get; set; }
        /// <summary>
        /// The set name color of this vehicle
        /// </summary>
        public SavedColor? NameColor { get; set; }
    }
}
