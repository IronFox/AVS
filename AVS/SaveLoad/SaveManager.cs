using AVS.BaseVehicle;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using batteries = System.Collections.Generic.List<System.Tuple<string, float>>;
using color = System.Tuple<float, float, float, float>;
using techtype = string;

namespace AVS.SaveLoad;

// see SaveData.cs
internal static class SaveManager
{
    /* Things what we can serialize
     * List<Tuple<Vector,Vector>>
     * List<Dictionary<Vector3, Vector3>>
     * List<Tuple<Dictionary<Vector3, Vector3>, Vector3>>
     */
    /* Things what we cannot get away with
     * List<Tuple<Dictionary<Vector3, Vector3>, TechType>>
     * List<TechType>
     */
    internal static bool MatchMv(AvsVehicle av, Vector3 location) =>
        // the following floats we compare should in reality be the same
        // but anyways there's probably no closer mod vehicle than 1 meter
        Vector3.Distance(av.transform.position, location) < 2;

    internal static List<Tuple<Vector3, Dictionary<string, techtype>>> SerializeUpgrades() => new();

    //internal static IEnumerator DeserializeUpgrades(SaveData data, AvsVehicle av)
    //{
    //    if (data.IsNull() || av.IsNull() || data.UpgradeLists.IsNull())
    //    {
    //        yield break;
    //    }
    //    // try to match against a saved vehicle in our list
    //    foreach (Tuple<Vector3, Dictionary<string, techtype>> tup in data.UpgradeLists)
    //    {
    //        if (MatchMv(av, tup.Item1))
    //        {
    //            foreach (KeyValuePair<string, techtype> pair in tup.Item2)
    //            {
    //                TaskResult<GameObject> result = new TaskResult<GameObject>();
    //                bool resulty = TechTypeExtensions.FromString(pair.Value, out TechType thisTT, true);
    //                if (!resulty)
    //                {
    //                    continue;
    //                }
    //                yield return CraftData.InstantiateFromPrefabAsync(thisTT, result, false);
    //                try
    //                {
    //                    GameObject thisUpgrade = result.Get();
    //                    thisUpgrade.transform.SetParent(av.modulesRoot.transform);
    //                    thisUpgrade.SetActive(false);
    //                    InventoryItem thisItem = new InventoryItem(thisUpgrade.GetComponent<Pickupable>());
    //                    av.modules.AddItem(pair.Key, thisItem, true);
    //                    // try calling OnUpgradeModulesChanged now
    //                    av.UpdateModuleSlots();
    //                }
    //                catch (Exception e)
    //                {
    //                    Logger.LogException($"Failed to load upgrades for {av.GetName()}", e);
    //                }
    //            }
    //        }
    //    }
    //}
    internal static List<Tuple<Vector3, List<Tuple<int, batteries>>>> SerializeModularStorage() => new();

    //internal static IEnumerator DeserializeModularStorage(SaveData data, AvsVehicle av)
    //{
    //    if (data.IsNull() || av.IsNull() || data.ModularStorages.IsNull())
    //    {
    //        yield break;
    //    }
    //    // try to match against a saved vehicle in our list
    //    foreach (Tuple<Vector3, List<Tuple<int, batteries>>> vehicle in data.ModularStorages)
    //    {
    //        if (MatchMv(av, vehicle.Item1))
    //        {
    //            // we've matched the vehicle
    //            foreach (var container in vehicle.Item2)
    //            {
    //                var thisContainer = av.ModGetStorageInSlot(container.Item1, TechType.VehicleStorageModule);
    //                if (thisContainer.IsNotNull())
    //                {
    //                    foreach (var techtype in container.Item2)
    //                    {
    //                        TaskResult<GameObject> result = new TaskResult<GameObject>();
    //                        bool resulty = TechTypeExtensions.FromString(techtype.Item1, out TechType thisTT, true);
    //                        if (!resulty)
    //                        {
    //                            continue;
    //                        }
    //                        yield return CraftData.InstantiateFromPrefabAsync(thisTT, result, false);
    //                        GameObject thisItem = result.Get();
    //                        if (techtype.Item2 >= 0)
    //                        {
    //                            // check whether we *are* a battery xor we *have* a battery
    //                            if (thisItem.GetComponent<Battery>().IsNotNull() && thisItem.GetComponentInChildren<Battery>().IsNotNull())
    //                            {
    //                                // we are a battery
    //                                thisItem.GetComponentInChildren<Battery>().charge = techtype.Item2;
    //                            }
    //                            else
    //                            {
    //                                // we have a battery (we are a tool)
    //                                // Thankfully we have this naming convention
    //                                Transform batSlot = thisItem.transform.Find("BatterySlot");
    //                                if (batSlot.IsNull())
    //                                {
    //                                    Logger.Warn($"Failed to load modular storage item {thisItem.name} to modular storage in vehicle {av.GetName()}.");
    //                                    continue;
    //                                }
    //                                result = new TaskResult<GameObject>();
    //                                yield return CraftData.InstantiateFromPrefabAsync(TechType.Battery, result, false);
    //                                GameObject newBat = result.Get();
    //                                if (newBat.GetComponent<Battery>().IsNotNull())
    //                                {
    //                                    newBat.GetComponent<Battery>().charge = techtype.Item2;
    //                                    Logger.Warn($"Failed to load modular storage battery {thisItem.name} to modular storage in vehicle {av.GetName()}.");
    //                                }
    //                                newBat.transform.SetParent(batSlot);
    //                                newBat.SetActive(false);
    //                            }
    //                        }
    //                        thisItem.transform.SetParent(av.Com.StorageRootObject.transform);
    //                        try
    //                        {
    //                            thisContainer.AddItem(thisItem.GetComponent<Pickupable>());
    //                        }
    //                        catch (Exception e)
    //                        {
    //                            Logger.LogException($"Failed to add storage item {thisItem.name} to modular storage in vehicle {av.GetName()}.", e);
    //                        }
    //                        thisItem.SetActive(false);
    //                    }
    //                }
    //                else
    //                {
    //                    Logger.Warn($"Tried to deserialize items into a non-existent modular container: {container.Item1.ToString()}");
    //                }
    //            }
    //        }
    //    }
    //}
    internal static List<Tuple<Vector3, List<Tuple<Vector3, batteries>>>> SerializeInnateStorage() => new();

    //internal static IEnumerator DeserializeInnateStorage(SaveData data, AvsVehicle av)
    //{
    //    if (data.IsNull() || av.IsNull() || data.InnateStorages.IsNull())
    //    {
    //        yield break;
    //    }
    //    // try to match against a saved vehicle in our list
    //    foreach (Tuple<Vector3, List<Tuple<Vector3, batteries>>> vehicle in data.InnateStorages)
    //    {
    //        if (MatchMv(av, vehicle.Item1))
    //        {
    //            foreach (var thisStorage in vehicle.Item2)
    //            {
    //                bool isStorageMatched = false;
    //                if (av.GetComponentsInChildren<InnateStorageContainer>().Count() == 0)
    //                {
    //                    continue;
    //                }
    //                // load up the storages
    //                foreach (var isc in av.GetComponentsInChildren<InnateStorageContainer>())
    //                {
    //                    isStorageMatched = false;
    //                    Vector3 thisLocalPos = av.transform.InverseTransformPoint(isc.transform.position);
    //                    if (Vector3.Distance(thisLocalPos, thisStorage.Item1) < 0.05f) // this is a weird amount of drift, but I'm afraid to use ==
    //                    {
    //                        isStorageMatched = true;
    //                        yield return MainPatcher.Instance.StartCoroutine(LoadThisStorage(av, thisStorage.Item2, isc));
    //                        break;
    //                    }
    //                }
    //                if (!isStorageMatched)
    //                {
    //                    Logger.Warn($"Failed to normally restore the contents of the {av.GetName()}. Trying the old method.");
    //                    foreach (var isc in av.GetComponentsInChildren<InnateStorageContainer>())
    //                    {
    //                        isStorageMatched = false;
    //                        if (Vector3.Distance(isc.transform.position, thisStorage.Item1) < 0.1f) // this is a weird amount of drift, but I'm afraid to use ==
    //                        {
    //                            isStorageMatched = true;
    //                            yield return MainPatcher.Instance.StartCoroutine(LoadThisStorage(av, thisStorage.Item2, isc));
    //                            break;
    //                        }
    //                    }
    //                    if (isStorageMatched)
    //                    {
    //                        Logger.Log("Successfully loaded contents. Will update the save data schema on next save.");
    //                    }
    //                }
    //                if (!isStorageMatched)
    //                {
    //                    Logger.Error($"Failed to restore the contents of the {av.GetName()}.");
    //                }
    //            }
    //        }
    //    }
    //    yield break;
    //}
    internal static List<Tuple<Vector3, batteries>> SerializeBatteries() => new();

    //internal static IEnumerator DeserializeBatteries(SaveData data, AvsVehicle av)
    //{
    //    if (data.IsNull() || av.IsNull() || data.Batteries.IsNull())
    //    {
    //        yield break;
    //    }
    //    // try to match against a saved vehicle in our list
    //    foreach (Tuple<Vector3, batteries> vehicle in data.Batteries)
    //    {
    //        if (MatchMv(av, vehicle.Item1))
    //        {
    //            foreach (var battery in vehicle.Item2.Select((value, i) => (value, i)))
    //            {
    //                TaskResult<GameObject> result = new TaskResult<GameObject>();
    //                bool resulty = TechTypeExtensions.FromString(battery.value.Item1, out TechType thisTT, true);
    //                if (!resulty)
    //                {
    //                    continue;
    //                }
    //                yield return CraftData.InstantiateFromPrefabAsync(thisTT, result, false);
    //                GameObject thisItem = result.Get();
    //                try
    //                {
    //                    thisItem.GetComponent<Battery>().charge = battery.value.Item2;
    //                    thisItem.transform.SetParent(av.Com.StorageRootObject.transform);
    //                    av.Com.Batteries[battery.i].BatterySlot.gameObject.GetComponent<EnergyMixin>().battery = thisItem.GetComponent<Battery>();
    //                    av.Com.Batteries[battery.i].BatterySlot.gameObject.GetComponent<EnergyMixin>().batterySlot.AddItem(thisItem.GetComponent<Pickupable>());
    //                    thisItem.SetActive(false);
    //                }
    //                catch (Exception e)
    //                {
    //                    Logger.LogException($"Failed to load battery {thisItem.name} into vehicle : {av.GetName()}.", e);
    //                }
    //            }
    //        }
    //    }
    //    yield break;
    //}
    internal static List<Tuple<Vector3, batteries>> SerializeBackupBatteries() => new();

    //internal static IEnumerator DeserializeBackupBatteries(SaveData data, Submarine av)
    //{
    //    if (data.IsNull() || av.IsNull() || data.BackupBatteries.IsNull())
    //    {
    //        yield break;
    //    }
    //    // try to match against a saved vehicle in our list
    //    foreach (Tuple<Vector3, batteries> slot in data.BackupBatteries)
    //    {
    //        if (MatchMv(av, slot.Item1))
    //        {
    //            if (av.Com.BackupBatteries.Count == 0)
    //            {
    //                continue;
    //            }
    //            foreach (var battery in slot.Item2.Select((value, i) => (value, i)))
    //            {
    //                TaskResult<GameObject> result = new TaskResult<GameObject>();
    //                bool resulty = TechTypeExtensions.FromString(battery.value.Item1, out TechType thisTT, true);
    //                if (!resulty)
    //                {
    //                    continue;
    //                }
    //                yield return CraftData.InstantiateFromPrefabAsync(thisTT, result, false);
    //                GameObject thisItem = result.Get();
    //                try
    //                {
    //                    thisItem.GetComponent<Battery>().charge = battery.value.Item2;
    //                    thisItem.transform.SetParent(av.Com.StorageRootObject.transform);
    //                    av.Com.BackupBatteries[battery.i].BatterySlot.gameObject.GetComponent<EnergyMixin>().battery = thisItem.GetComponent<Battery>();
    //                    av.Com.BackupBatteries[battery.i].BatterySlot.gameObject.GetComponent<EnergyMixin>().batterySlot.AddItem(thisItem.GetComponent<Pickupable>());
    //                    thisItem.SetActive(false);
    //                }
    //                catch (Exception e)
    //                {
    //                    Logger.LogException($"Failed to load battery {thisItem.name} into vehicle : {av.GetName()}.", e);
    //                }
    //            }
    //        }
    //    }
    //    yield break;
    //}
    internal static List<Tuple<Vector3, bool>> SerializePlayerInside() => new();

    //internal static IEnumerator DeserializePlayerInside(SaveData data, AvsVehicle av)
    //{
    //    if (data.IsNull() || av.IsNull() || data.IsPlayerInside.IsNull())
    //    {
    //        yield break;
    //    }
    //    foreach (Tuple<Vector3, bool> vehicle in data.IsPlayerInside)
    //    {
    //        if (MatchMv(av, vehicle.Item1) && vehicle.Item2)
    //        {
    //            try
    //            {
    //                av.PlayerEntry();
    //            }
    //            catch (Exception e)
    //            {
    //                Logger.LogException($"Failed to load player into vehicle : {av.GetName()}.", e);
    //            }
    //            yield break;
    //        }
    //    }
    //}
    internal static List<Tuple<Vector3, string, color, color, color, color, bool>> SerializeAesthetics() => new();
    //internal static IEnumerator DeserializeAesthetics(SaveData data, AvsVehicle av)
    //{
    //    if (data.IsNull() || av.IsNull() || data.AllVehiclesAesthetics.IsNull())
    //    {
    //        yield break;
    //    }
    //    VehicleColor SynthesizeColor(color col)
    //    {
    //        return new VehicleColor(new Color(col.Item1, col.Item2, col.Item3, col.Item4));
    //    }
    //    foreach (Tuple<Vector3, string, color, color, color, color, bool> vehicle in data.AllVehiclesAesthetics)
    //    {
    //        if (MatchMv(av, vehicle.Item1))
    //        {
    //            try
    //            {
    //                if (av is Submarine mvSub && mvSub.Com.ColorPicker.IsNotNull())
    //                {
    //                    var active = mvSub.Com.ColorPicker.transform.Find("EditScreen/Active");
    //                    if (active is null)
    //                    {
    //                        continue;
    //                    }
    //                    active.transform.Find("InputField").GetComponent<uGUI_InputField>().text = vehicle.Item2;
    //                    active.transform.Find("InputField/Text").GetComponent<TMPro.TextMeshProUGUI>().text = vehicle.Item2;
    //                    mvSub.SetName(vehicle.Item2);
    //                    if (vehicle.Item7)
    //                    {
    //                        mvSub.PaintVehicleDefaultStyle(vehicle.Item2);
    //                        mvSub.OnNameChange(vehicle.Item2);
    //                    }
    //                    else
    //                    {
    //                        mvSub.SetBaseColor(SynthesizeColor(vehicle.Item3));
    //                        mvSub.SetInteriorColor(SynthesizeColor(vehicle.Item4));
    //                        mvSub.SetStripeColor(SynthesizeColor(vehicle.Item5));
    //                        mvSub.SetNameColor(SynthesizeColor(vehicle.Item6));
    //                        mvSub.PaintVehicleSection("ExteriorMainColor", mvSub.BaseColor);
    //                        mvSub.PaintVehicleSection("ExteriorPrimaryAccent", mvSub.InteriorColor);
    //                        mvSub.PaintVehicleSection("ExteriorSecondaryAccent", mvSub.StripeColor);
    //                        mvSub.PaintVehicleName(vehicle.Item2, mvSub.NameColor, mvSub.BaseColor);

    //                        mvSub.IsDefaultTexture = false;

    //                        active.transform.Find("MainExterior/SelectedColor").GetComponent<Image>().color = mvSub.BaseColor.RGB;
    //                        active.transform.Find("PrimaryAccent/SelectedColor").GetComponent<Image>().color = mvSub.InteriorColor.RGB;
    //                        active.transform.Find("SecondaryAccent/SelectedColor").GetComponent<Image>().color = mvSub.StripeColor.RGB;
    //                        active.transform.Find("NameLabel/SelectedColor").GetComponent<Image>().color = mvSub.NameColor.RGB;
    //                    }
    //                }
    //                else
    //                {
    //                    av.SetBaseColor(SynthesizeColor(vehicle.Item3));
    //                    av.SetInteriorColor(SynthesizeColor(vehicle.Item4));
    //                    av.SetStripeColor(SynthesizeColor(vehicle.Item5));
    //                    av.SetNameColor(SynthesizeColor(vehicle.Item6));

    //                    av.subName.SetColor(0, Vector3.zero, av.BaseColor.RGB);
    //                    av.subName.SetColor(1, Vector3.zero, av.NameColor.RGB);
    //                    av.subName.SetColor(2, Vector3.zero, av.InteriorColor.RGB);
    //                    av.subName.SetColor(3, Vector3.zero, av.StripeColor.RGB);
    //                }
    //                break;
    //            }
    //            catch (Exception e)
    //            {
    //                Logger.LogException($"Failed to load color details for vehicle : {av.GetName()}.", e);
    //            }
    //        }
    //    }
    //}
    internal static List<Tuple<Vector3, bool>> SerializePlayerControlling() => new();

    //internal static IEnumerator DeserializePlayerControlling(SaveData data, AvsVehicle av)
    //{
    //    if (data.IsNull() || av.IsNull() || data.IsPlayerControlling.IsNull())
    //    {
    //        yield break;
    //    }
    //    foreach (Tuple<Vector3, bool> vehicle in data.IsPlayerControlling)
    //    {
    //        if (MatchMv(av, vehicle.Item1) && vehicle.Item2)
    //        {
    //            try
    //            {
    //                av.BeginHelmControl();
    //            }
    //            catch (Exception e)
    //            {
    //                Logger.LogException($"Failed to load player into vehicle : {av.GetName()}.", e);
    //            }
    //            yield break;
    //        }
    //    }
    //}
    internal static List<Tuple<Vector3, string>> SerializeSubName() => new();
    //internal static IEnumerator DeserializeSubName(SaveData data, AvsVehicle av)
    //{
    //    if (data.IsNull() || av.IsNull() || data.SubNames.IsNull())
    //    {
    //        yield break;
    //    }
    //    foreach (Tuple<Vector3, string> vehicle in data.SubNames)
    //    {
    //        if (MatchMv(av, vehicle.Item1))
    //        {
    //            try
    //            {
    //                av.subName.SetName(vehicle.Item2);
    //            }
    //            catch (Exception e)
    //            {
    //                Logger.LogException($"Failed to load SubName for vehicle : {av.GetName()}.", e);
    //            }
    //            yield break;
    //        }
    //    }
    //}
    // internal static IEnumerator LoadThisStorage(AvsVehicle av, batteries thisStorage, InnateStorageContainer matchedContainer)
    // {
    //     foreach (var techtype in thisStorage)
    //     {
    //         TaskResult<GameObject> result = new TaskResult<GameObject>();
    //         System.String techTypeString = techtype.Item1.Replace("Undiscovered", ""); // fix for yet-"undiscovered" creature eggs
    //         bool resulty = TechTypeExtensions.FromString(techTypeString, out TechType thisTT, true);
    //         if (!resulty)
    //         {
    //             continue;
    //         }
    //         yield return CraftData.InstantiateFromPrefabAsync(thisTT, result, false);
    //         GameObject thisItem = result.Get();
    //         if (techtype.Item2 >= 0)
    //         {
    //             // check whether we *are* a battery xor we *have* a battery
    //             if (thisItem.GetComponent<Battery>().IsNotNull() && thisItem.GetComponentInChildren<Battery>().IsNotNull())
    //             {
    //                 // we are a battery
    //                 thisItem.GetComponentInChildren<Battery>().charge = techtype.Item2;
    //             }
    //             else
    //             {
    //                 // we have a battery (we are a tool)
    //                 // Thankfully we have this naming convention
    //                 Transform batSlot = thisItem.transform.Find("BatterySlot");
    //                 if (batSlot.IsNull())
    //                 {
    //                     Logger.Warn($"Failed to load innate storage item {thisItem.name} to modular storage for {av.name} : {av.GetName()}.");
    //                     continue;
    //                 }
    //                 result = new TaskResult<GameObject>();
    //                 yield return CraftData.InstantiateFromPrefabAsync(TechType.Battery, result, false);
    //                 GameObject newBat = result.Get();
    //                 if (newBat.GetComponent<Battery>().IsNotNull())
    //                 {
    //                     newBat.GetComponent<Battery>().charge = techtype.Item2;
    //                     Logger.Warn($"Failed to load innate storage battery {thisItem.name} to modular storage for {av.name} : {av.GetName()}.");
    //                 }
    //                 newBat.transform.SetParent(batSlot);
    //                 newBat.SetActive(false);
    //             }
    //         }
    //         thisItem.transform.SetParent(av.Com.StorageRootObject.transform);
    //         try
    //         {
    //             matchedContainer.Container.AddItem(thisItem.GetComponent<Pickupable>());
    //         }
    //         catch (Exception e)
    //         {
    //             Logger.LogException($"Failed to add storage item {thisItem.name} to modular storage for {av.name} : {av.GetName()}.", e);
    //         }
    //         thisItem.SetActive(false);
    //     }
    //
    //
    //
    // }
}