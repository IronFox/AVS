namespace AVS.MaterialAdapt
{
    /// <summary>
    /// The classification of a material for adaptation purposes.
    /// </summary>
    public readonly struct MaterialClassification
    {
        /// <summary>
        /// The type of material to apply.
        /// </summary>
        public MaterialType Type { get; }
        /// <summary>
        /// Gets a value indicating whether shader names should be ignored during processing.
        /// </summary>
        public bool IgnoreShaderNames { get; }
        /// <summary>
        /// True if the source material should be included in the adaptation process.
        /// </summary>
        public bool Include { get; }

        /// <summary>
        /// Constructs an included material classification with the specified type and shader name handling.
        /// </summary>
        /// <param name="type">The type of material to apply.</param>
        /// <param name="ignoreShaderNames">Whether shader names should be ignored during processing</param>
        public MaterialClassification(MaterialType type, bool ignoreShaderNames = false)
        {
            Type = type;
            IgnoreShaderNames = ignoreShaderNames;
            Include = true;
        }

        /// <summary>
        /// Global excluded material classification.
        /// </summary>
        public static MaterialClassification Excluded { get; } = default;
    }
}