namespace AVS.SaveLoad
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
        /// The hatch through which the player entered this vehicle.
        /// -1 if the player is not inside this vehicle.
        /// </summary>
        public int EnteredThroughHatch { get; set; }
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
