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
        /// Initializes a new instance of the <see cref="PatcherImages"/> class with specified images for depth module
        /// icons.
        /// </summary>
        /// <param name="depthModule1Icon">The image representing the icon for the first depth module.</param>
        /// <param name="depthModule2Icon">The image representing the icon for the second depth module.</param>
        /// <param name="depthModule3Icon">The image representing the icon for the third depth module.</param>
        /// <param name="depthModuleNodeIcon">The image representing the icon for the depth module node.</param>
        public PatcherImages(Image depthModule1Icon,
                             Image depthModule2Icon,
                             Image depthModule3Icon,
                             Image depthModuleNodeIcon)
        {
            DepthModule1Icon = depthModule1Icon;
            DepthModule2Icon = depthModule2Icon;
            DepthModule3Icon = depthModule3Icon;
            DepthModuleNodeIcon = depthModuleNodeIcon;
        }
    }
}
