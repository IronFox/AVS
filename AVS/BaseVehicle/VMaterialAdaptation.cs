using AVS.Configuration;
using AVS.MaterialAdapt;

namespace AVS.BaseVehicle
{
    public abstract partial class AvsVehicle
    {

        /// <summary>
        /// The material fixer instance used for this vehicle.
        /// Ineffective if <see cref="VehicleConfiguration.AutoFixMaterials"/> is false.
        /// </summary>
        public MaterialFixer MaterialFixer { get; }




    }
}
