using System.Collections.Generic;

namespace AVS.UpgradeModules
{
    internal abstract class DepthModuleBase : AvsVehicleModule
    {

        internal static List<TechType> AllDepthModuleTypes { get; } = new List<TechType>();

        /// <inheritdoc/>
        public override IReadOnlyCollection<TechType>? AutoDisplace => AllDepthModuleTypes;

    }
}