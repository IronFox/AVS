using AVS.VehicleComponents;

namespace AVS.BaseVehicle
{
    public abstract partial class AvsVehicle
    {
        private VehicleColor baseColor = VehicleColor.Default;
        private VehicleColor interiorColor = VehicleColor.Default;
        private VehicleColor stripeColor = VehicleColor.Default;
        private VehicleColor nameColor = VehicleColor.Default;

        /// <summary>
        /// Updates the base color of the vehicle.
        /// </summary>
        public virtual void SetBaseColor(VehicleColor color)
        {
            baseColor = color;
        }

        /// <summary>
        /// Updates the interior color of the vehicle.
        /// </summary>
        public virtual void SetInteriorColor(VehicleColor color)
        {
            interiorColor = color;
        }
        /// <summary>
        /// Updates the stripe color of the vehicle.
        /// </summary>
        public virtual void SetStripeColor(VehicleColor color)
        {
            stripeColor = color;
        }
        /// <summary>
        /// Updates the name color of the vehicle.
        /// </summary>
        public virtual void SetNameColor(VehicleColor color)
        {
            nameColor = color;
        }

        /// <summary>
        /// The current base color.
        /// </summary>
        public VehicleColor BaseColor => baseColor;
        /// <summary>
        /// The current interior color.
        /// </summary>
        public VehicleColor InteriorColor => interiorColor;
        /// <summary>
        /// The current stripe color.
        /// </summary>
        public VehicleColor StripeColor => stripeColor;
        /// <summary>
        /// The current name color.
        /// </summary>
        public VehicleColor NameColor => nameColor;
    }
}
