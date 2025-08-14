namespace AVS.Assets
{
    /// <summary>
    /// Represents a collection of images used by AVS.
    /// </summary>
    public class PatcherImages
    {
        /// <summary>
        /// The icon for the Depth Module 1 upgrade.
        /// </summary>
        public Image DepthModule1Icon { get; }
        /// <summary>
        /// The icon for the Depth Module 2 upgrade.
        /// </summary>
        public Image DepthModule2Icon { get; }
        /// <summary>
        /// The icon for the Depth Module 3 upgrade.
        /// </summary>
        public Image DepthModule3Icon { get; }
        /// <summary>
        /// The icon to use for the parent node of all depth modules in the crafting tree.
        /// </summary>
        public Image DepthModuleNodeIcon { get; }
        /// <summary>
        /// Icon used by the AVS fabricator.
        /// </summary>
        public Image FabricatorIcon { get; }

        /// <summary>
        /// Background image for the updates module panel.
        /// </summary>
        public Image ModulesBackground { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PatcherImages"/> class with specified images for depth module
        /// icons.
        /// </summary>
        public PatcherImages(Image depthModule1Icon,
                             Image depthModule2Icon,
                             Image depthModule3Icon,
                             Image depthModuleNodeIcon,
                             Image fabricatorIcon,
                             Image modulesBackground
                             )
        {
            FabricatorIcon = fabricatorIcon;
            DepthModule1Icon = depthModule1Icon;
            DepthModule2Icon = depthModule2Icon;
            DepthModule3Icon = depthModule3Icon;
            DepthModuleNodeIcon = depthModuleNodeIcon;
            ModulesBackground = modulesBackground;
        }
    }
}
