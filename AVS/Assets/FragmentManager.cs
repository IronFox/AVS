using AVS.Util;
using Nautilus.Assets.Gadgets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.Assets;

/// <summary>
/// Possible spawn location for fragments.
/// </summary>
public readonly struct FragmentSpawnLocation
{
    /// <summary>
    /// Spawn position.
    /// </summary>
    public Vector3 Position { get; }

    /// <summary>
    /// Spawn euler angles.
    /// </summary>
    public Vector3 EulerAngles { get; }

    /// <summary>
    /// Creates a new spawn location for fragments.
    /// </summary>
    public FragmentSpawnLocation(Vector3 position, Vector3 eulerAngles = default)
    {
        Position = position;
        EulerAngles = eulerAngles;
    }
}

/// <summary>
/// FragmentData is a struct that contains all the information needed to register a fragment in the game.
/// </summary>
public struct FragmentData
{
    /// <summary>
    /// Fragment variations to use. The first one will be used as the main fragment, the rest will be used as variations.
    /// </summary>
    public IReadOnlyList<GameObject> Fragments { get; }

    /// <summary>
    /// The tech type that will be unlocked when the fragment is scanned.
    /// </summary>
    public TechType Unlocks { get; }

    /// <summary>
    /// Number of fragments that need to be scanned to unlock the tech type.
    /// </summary>
    public int FragmentsToScan { get; }

    /// <summary>
    /// The scan time in seconds for each fragment.
    /// </summary>
    public float ScanTime { get; }

    /// <summary>
    /// The unique class ID of the fragment.
    /// </summary>
    public string ClassID { get; }

    /// <summary>
    /// The display text for the fragment.
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    /// The description text for the fragment, shown in the PDA.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Spawn locations for the fragment. If there are multiple fragments, they will be spawned in a round-robin fashion.
    /// </summary>
    public IReadOnlyList<FragmentSpawnLocation> SpawnLocations { get; }

    /// <summary>
    /// The encyclopedia key for the fragment, used to link it to the encyclopedia entry.
    /// </summary>
    public string EncyclopediaKey { get; }

    /// <summary>
    /// Creates a new FragmentData instance.
    /// </summary>
    /// <param name="fragments">Fragment variations to use. The first one will be used as the main fragment, the rest will be used as variations</param>
    /// <param name="unlocks">The tech type that will be unlocked when the fragment is scanned</param>
    /// <param name="fragmentsToScan">Number of fragments that need to be scanned to unlock the tech type</param>
    /// <param name="scanTime">The scan time in seconds for each fragment</param>
    /// <param name="classID">The unique class ID of the fragment</param>
    /// <param name="displayName">The display text for the fragment</param>
    /// <param name="description">The description text for the fragment, shown in the PDA</param>
    /// <param name="spawnLocations">Spawn locations for the fragment. If there are multiple fragments, they will be spawned in a round-robin fashion</param>
    /// <param name="encyKey">The encyclopedia key for the fragment, used to link it to the encyclopedia entry</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public FragmentData(IReadOnlyList<GameObject> fragments, TechType unlocks, int fragmentsToScan, float scanTime,
        string classID, string displayName, string description,
        IReadOnlyList<FragmentSpawnLocation>? spawnLocations = null, string encyKey = "")
    {
        Fragments = fragments ?? throw new ArgumentNullException(nameof(fragments), "Fragment list must not be empty");
        if (fragments.Count == 0)
            throw new ArgumentException("Fragment list must not be empty", nameof(fragments));
        Unlocks = unlocks;
        FragmentsToScan = fragmentsToScan;
        ScanTime = scanTime;
        ClassID = classID;
        DisplayName = displayName;
        Description = description;
        SpawnLocations = spawnLocations ??
                         throw new ArgumentException($"Trying to register fragment with no spawn locations");
        EncyclopediaKey = encyKey;
    }
}

/// <summary>
/// Management class for fragments, scattered across the map.
/// Use this to register fragments and their properties.
/// </summary>
public class FragmentManager : MonoBehaviour
{
    private static readonly List<PDAScanner.EntryData> PDAScannerData = new();

    internal static PDAScanner.EntryData MakeGenericEntryData(TechType fragmentTT, FragmentData frag)
    {
        var entryData = new PDAScanner.EntryData()
        {
            key = fragmentTT,
            locked = true,
            totalFragments = frag.FragmentsToScan,
            destroyAfterScan = true,
            encyclopedia = frag.EncyclopediaKey,
            blueprint = frag.Unlocks,
            scanTime = frag.ScanTime,
            isFragment = true
        };
        return entryData;
    }

    /// <summary>
    /// Registers a fragment using a FragmentData struct as input. For an AvsVehicle, you can access its techtype AFTER registration like this:
    /// vehicle.GetComponent&lt;TechTag&gt;().type
    /// </summary>
    /// <returns>The TechType of the new fragment.</returns>
    public static TechType RegisterFragment(FragmentData frag)
    {
        if (frag.Fragments.Count == 0)
        {
            Logger.Error("RegisterFragment error: no fragment objects were supplied");
            return 0;
        }

        if (frag.SpawnLocations.Count == 0)
        {
            Logger.Error("For classID: " + frag.ClassID + ": Tried to register fragment without any spawn locations!");
            return 0;
        }

        TechType fragmentTT;
        if (frag.Fragments.Count == 1)
        {
            var customPrefab = RegisterFragmentGenericSingle(frag, frag.Fragments[0], true, out fragmentTT);
            customPrefab.Register();
            Logger.Log("Registered fragment: " + frag.ClassID);
        }
        else
        {
            fragmentTT = RegisterFragmentGeneric(frag);
            Logger.Log($" Registered fragment: {frag.ClassID} with {frag.Fragments.Count + 1} variations.");
        }

        PDAScannerData.Add(MakeGenericEntryData(fragmentTT, frag));
        return fragmentTT;
    }

    internal static TechType RegisterFragmentGeneric(FragmentData frag)
    {
        var head = frag.Fragments.First();
        var tail = frag.Fragments.Skip(1);
        TechType fragmentType;
        var customPrefabs = new List<Nautilus.Assets.CustomPrefab>();
        customPrefabs.Add(RegisterFragmentGenericSingle(frag, head, false, out fragmentType));
        var numberFragments = 1;
        foreach (var fragmentObject in tail)
        {
            Shaders.ApplyMainShaderRecursively(fragmentObject);
            var fragmentInfo = Nautilus.Assets.PrefabInfo.WithTechType(frag.ClassID + numberFragments, frag.DisplayName,
                frag.Description);
            numberFragments++;
            fragmentInfo.TechType = fragmentType;
            var customFragmentPrefab = new Nautilus.Assets.CustomPrefab(fragmentInfo);
            fragmentObject.EnsureComponent<BoxCollider>();
            fragmentObject.EnsureComponent<PrefabIdentifier>().ClassId = frag.ClassID;
            fragmentObject.EnsureComponent<FragmentManager>();
            fragmentObject.EnsureComponent<LargeWorldEntity>();
            fragmentObject.EnsureComponent<SkyApplier>().enabled = true;
            SetupScannable(fragmentObject, fragmentInfo.TechType);
            customFragmentPrefab.SetGameObject(() => fragmentObject);
            customPrefabs.Add(customFragmentPrefab);
        }

        var numberPrefabsRegistered = 0;
        foreach (var customPrefab in customPrefabs)
        {
            var spawnLocationsToUse = new List<FragmentSpawnLocation>();
            var iterator = numberPrefabsRegistered;
            while (iterator < frag.SpawnLocations.Count)
            {
                spawnLocationsToUse.Add(frag.SpawnLocations[iterator]);
                iterator += customPrefabs.Count;
            }

            customPrefab.SetSpawns(
                spawnLocationsToUse
                    .Select(x => new Nautilus.Assets.SpawnLocation(x.Position, x.EulerAngles))
                    .ToArray()
            ); // this creates a harmless Nautilus error
            customPrefab.Register();
            numberPrefabsRegistered++;
        }

        return fragmentType;
    }

    internal static Nautilus.Assets.CustomPrefab RegisterFragmentGenericSingle(FragmentData frag,
        GameObject fragmentObject, bool doSpawnLocations, out TechType result)
    {
        Shaders.ApplyMainShaderRecursively(fragmentObject);
        var fragmentInfo = Nautilus.Assets.PrefabInfo.WithTechType(frag.ClassID, frag.DisplayName, frag.Description);
        var customFragmentPrefab = new Nautilus.Assets.CustomPrefab(fragmentInfo);
        fragmentObject.EnsureComponent<BoxCollider>();
        Nautilus.Utility.PrefabUtils.AddBasicComponents(fragmentObject, frag.ClassID, fragmentInfo.TechType,
            LargeWorldEntity.CellLevel.Global);
        fragmentObject.EnsureComponent<FragmentManager>();
        SetupScannable(fragmentObject, fragmentInfo.TechType);
        customFragmentPrefab.SetGameObject(() => fragmentObject);
        if (doSpawnLocations)
            customFragmentPrefab
                .SetSpawns(frag.SpawnLocations
                    .Select(x => new Nautilus.Assets.SpawnLocation(x.Position, x.EulerAngles))
                    .ToArray()
                ); // this creates a harmless Nautilus error
        result = fragmentInfo.TechType;
        return customFragmentPrefab;
    }

    internal static void AddScannerDataEntries()
    {
        void TryAddScannerData(PDAScanner.EntryData data)
        {
            if (PDAScanner.mapping.ContainsKey(data.key))
                return;
            PDAScanner.mapping.Add(data.key, data);
        }

        PDAScannerData.ForEach(x => TryAddScannerData(x));
    }

    /// <inheritdoc />
    public void Start()
    {
        IEnumerator DestroyPickupable()
        {
            while (GetComponent<Pickupable>().IsNotNull())
            {
                Destroy(GetComponent<Pickupable>());
                yield return null;
            }

            Destroy(this);
        }

        StartCoroutine(DestroyPickupable());
    }

    internal static void SetupScannable(GameObject obj, TechType tt)
    {
        var rt = obj.EnsureComponent<ResourceTracker>();
        rt.techType = tt;
        rt.prefabIdentifier = obj.GetComponent<PrefabIdentifier>();
        rt.prefabIdentifier.Id = "";
        rt.overrideTechType = TechType.None;
    }
}