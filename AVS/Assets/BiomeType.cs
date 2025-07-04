using System.Collections.Generic;
using System.Linq;
using BiomeData = LootDistributionData.BiomeData;

namespace AVS.Assets
{
    /// <summary>
    /// Biome types used in Subnautica.
    /// </summary>
    public enum AbstractBiomeType
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        SafeShallows,
        KelpForest,
        GrassyPlateus,
        MushroomForest,
        BulbZone,
        JellyshroomCaves,
        FloatingIslands,
        LavaZone,
        CrashZone,
        SparseReef,
        UnderwaterIslands,
        GrandReef,
        DeepGrandReef,
        BloodKelp,
        Mountains,
        Dunes,
        SeaTreader,
        TreeCove,
        BonesField,
        GhostTree,
        LostRiver1,
        LostRiver2,
        Canyon,
        SkeletonCave,
        CragField,
        PrisonAquarium,
        Mesas
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    /// <summary>
    /// Provides methods and mappings for working with biome types and their associated data.
    /// </summary>
    /// <remarks>The <see cref="BiomeTypes"/> class offers functionality to retrieve biome types associated
    /// with  specific abstract biome categories, as well as methods to generate biome data based on input parameters. 
    /// This class is designed to facilitate operations involving biomes, such as retrieving biome lists or  creating
    /// biome-specific data structures.</remarks>
    public static class BiomeTypes
    {
        // I included "CreatureOnly" biomes and some dead (unused) biome numbers
        private static Dictionary<AbstractBiomeType, List<BiomeType>> mapping = new Dictionary<AbstractBiomeType, List<BiomeType>>
        {
            {AbstractBiomeType.SafeShallows, Enumerable.Range(101, 27).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.KelpForest, Enumerable.Range(201, 22).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.GrassyPlateus, Enumerable.Range(301, 26).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.MushroomForest, Enumerable.Range(401, 31).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.BulbZone, Enumerable.Range(501, 21).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.JellyshroomCaves, Enumerable.Range(601, 12).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.FloatingIslands, Enumerable.Range(701, 7).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.LavaZone, Enumerable.Range(801, 40).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.CrashZone, Enumerable.Range(913, 4).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.SparseReef, Enumerable.Range(1005, 20).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.UnderwaterIslands, Enumerable.Range(1200, 19).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.GrandReef, Enumerable.Range(1300, 21).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.DeepGrandReef, Enumerable.Range(1400, 9).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.BloodKelp, Enumerable.Range(1500, 21).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.Mountains, Enumerable.Range(1600, 19).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.Dunes, Enumerable.Range(1700, 19).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.SeaTreader, Enumerable.Range(1800, 13).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.TreeCove, Enumerable.Range(1900, 7).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.BonesField, Enumerable.Range(2000, 24).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.GhostTree, Enumerable.Range(2100, 14).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.LostRiver1, Enumerable.Range(2200, 9).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.LostRiver2, Enumerable.Range(2900, 9).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.Canyon, Enumerable.Range(2300, 6).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.SkeletonCave, Enumerable.Range(2400, 8).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.CragField, Enumerable.Range(2500, 4).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.PrisonAquarium, Enumerable.Range(2600, 9).Select(i => (BiomeType)i).ToList() },
            {AbstractBiomeType.Mesas, Enumerable.Range(2800, 3).Select(i => (BiomeType)i).ToList() }
        };

        /// <summary>
        /// Retrieves a read-only list of biome types associated with the specified abstract biome type.
        /// </summary>
        /// <param name="type">The abstract biome type for which to retrieve the associated biome types.</param>
        /// <returns>A read-only list of <see cref="BiomeType"/> objects associated with the specified  <paramref name="type"/>.
        /// The list will contain all biome types mapped to the given abstract biome type.</returns>
        public static IReadOnlyList<BiomeType> Get(AbstractBiomeType type)
        {
            return mapping[type];
        }

        private static BiomeData NewOneBiomeData(BiomeType type, int count = 1, float probability = 0.1f)
        {
            return new BiomeData { biome = type, count = count, probability = probability };
        }

        /// <summary>
        /// Retrieves a list of biome data based on the specified biome structure.
        /// </summary>
        /// <remarks>The method uses the type specified in <paramref name="biomeStruct"/> to determine the
        /// biome types and generates corresponding biome data entries based on the count and probability
        /// values.</remarks>
        /// <param name="biomeStruct">A structure containing the type, count, and probability information used to generate the biome data.</param>
        /// <returns>A read-only list of <see cref="BiomeData"/> objects representing the generated biome data.</returns>
        public static IReadOnlyList<BiomeData> GetBiomeData(BiomeStruct biomeStruct)
        {
            return Get(biomeStruct.Type).Select(biomeType => NewOneBiomeData(biomeType, biomeStruct.Count, biomeStruct.Probability)).ToList();
        }
    }

    /// <summary>
    /// Represents a biome with its type, count, and probability of loot occurrence in this biome.
    /// </summary>
    /// <remarks>This structure is immutable and is used to encapsulate information about a specific biome,
    /// including its type, the number of occurrences, and the likelihood of loot in it.</remarks>
    public readonly struct BiomeStruct
    {
        /// <summary>
        /// Gets the type of the biome represented by this instance.
        /// </summary>
        public AbstractBiomeType Type { get; }
        /// <summary>
        /// Probably the number of loot items spawned at once if this biome is selected by random based on Probability.
        /// </summary>
        public int Count { get; }
        /// <summary>
        /// Gets the probability value as a floating-point number.
        /// </summary>
        public float Probability { get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="BiomeStruct"/> class with the specified biome type, count, and
        /// probability.
        /// </summary>
        /// <param name="type">The type of the biome represented by this structure. This value cannot be null.</param>
        /// <param name="count">The number of occurrences or instances of the biome. Must be a non-negative integer.</param>
        /// <param name="probability">The probability associated with the biome, represented as a floating-point value between 0.0 and 1.0.</param>
        public BiomeStruct(AbstractBiomeType type, int count, float probability)
        {
            Type = type;
            Count = count;
            Probability = probability;
        }
    }


    /// <summary>
    /// Represents a collection of abstract biome data, allowing conversion to concrete biome data.
    /// </summary>
    public class AbstractBiomeData
    {
        /// <summary>
        /// Gets the list of <see cref="BiomeStruct"/> instances representing the abstract biomes.
        /// </summary>
        internal List<BiomeStruct> Biomes { get; } = new List<BiomeStruct>();

        /// <summary>
        /// Converts a <see cref="BiomeStruct"/> to a read-only list of <see cref="BiomeData"/> objects.
        /// </summary>
        /// <param name="biome">The <see cref="BiomeStruct"/> to convert.</param>
        /// <returns>A read-only list of <see cref="BiomeData"/> objects corresponding to the given biome structure.</returns>
        public IReadOnlyList<BiomeData> ConvertStruct(BiomeStruct biome)
        {
            return BiomeTypes.GetBiomeData(biome);
        }

        /// <summary>
        /// Retrieves all <see cref="BiomeData"/> objects for the biomes contained in this instance.
        /// </summary>
        /// <returns>A read-only list of <see cref="BiomeData"/> objects representing all biomes in this collection.</returns>
        public IReadOnlyList<BiomeData> Get()
        {
            return Biomes.SelectMany(x => ConvertStruct(x)).ToList();
        }
    }


    /// <summary>
    /// Extension methods for <see cref="AbstractBiomeData"/> to simplify adding biomes.
    /// </summary>
    public static class AbstractBiomeDataExtensions
    {
        /// <summary>
        /// Adds a new <see cref="BiomeStruct"/> to the <see cref="AbstractBiomeData"/> instance with the specified parameters.
        /// </summary>
        /// <param name="data">The <see cref="AbstractBiomeData"/> instance to add the biome to.</param>
        /// <param name="type">The abstract biome type to add.</param>
        /// <param name="count">The number of occurrences for the biome. Defaults to 1.</param>
        /// <param name="probability">The probability of loot occurrence in the biome. Defaults to 0.1.</param>
        /// <returns>The <see cref="AbstractBiomeData"/> instance with the new biome added.</returns>
        public static AbstractBiomeData WithBiome(this AbstractBiomeData data, AbstractBiomeType type, int count = 1, float probability = 0.1f)
        {
            data.Biomes.Add(new BiomeStruct(type, count, probability));
            return data;
        }
    }
}
