﻿using AVS.BaseVehicle;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using batteries = System.Collections.Generic.List<System.Tuple<System.String, float>>;
using color = System.Tuple<float, float, float, float>;
using techtype = System.String;

namespace AVS.SaveLoad
{
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
        internal static bool MatchMv(AvsVehicle mv, Vector3 location)
        {
            // the following floats we compare should in reality be the same
            // but anyways there's probably no closer mod vehicle than 1 meter
            return Vector3.Distance(mv.transform.position, location) < 2;
        }
        internal static List<Tuple<Vector3, Dictionary<string, techtype>>> SerializeUpgrades()
        {
            return new List<Tuple<Vector3, Dictionary<string, techtype>>>();
        }
        //internal static IEnumerator DeserializeUpgrades(SaveData data, ModVehicle mv)
        //{
        //    if (data == null || mv == null || data.UpgradeLists == null)
        //    {
        //        yield break;
        //    }
        //    // try to match against a saved vehicle in our list
        //    foreach (Tuple<Vector3, Dictionary<string, techtype>> tup in data.UpgradeLists)
        //    {
        //        if (MatchMv(mv, tup.Item1))
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
        //                    thisUpgrade.transform.SetParent(mv.modulesRoot.transform);
        //                    thisUpgrade.SetActive(false);
        //                    InventoryItem thisItem = new InventoryItem(thisUpgrade.GetComponent<Pickupable>());
        //                    mv.modules.AddItem(pair.Key, thisItem, true);
        //                    // try calling OnUpgradeModulesChanged now
        //                    mv.UpdateModuleSlots();
        //                }
        //                catch (Exception e)
        //                {
        //                    Logger.LogException($"Failed to load upgrades for {mv.GetName()}", e);
        //                }
        //            }
        //        }
        //    }
        //}
        internal static List<Tuple<Vector3, List<Tuple<int, batteries>>>> SerializeModularStorage()
        {
            return new List<Tuple<Vector3, List<Tuple<int, batteries>>>>();
        }
        //internal static IEnumerator DeserializeModularStorage(SaveData data, ModVehicle mv)
        //{
        //    if (data == null || mv == null || data.ModularStorages == null)
        //    {
        //        yield break;
        //    }
        //    // try to match against a saved vehicle in our list
        //    foreach (Tuple<Vector3, List<Tuple<int, batteries>>> vehicle in data.ModularStorages)
        //    {
        //        if (MatchMv(mv, vehicle.Item1))
        //        {
        //            // we've matched the vehicle
        //            foreach (var container in vehicle.Item2)
        //            {
        //                var thisContainer = mv.ModGetStorageInSlot(container.Item1, TechType.VehicleStorageModule);
        //                if (thisContainer != null)
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
        //                            if (thisItem.GetComponent<Battery>() != null && thisItem.GetComponentInChildren<Battery>() != null)
        //                            {
        //                                // we are a battery
        //                                thisItem.GetComponentInChildren<Battery>().charge = techtype.Item2;
        //                            }
        //                            else
        //                            {
        //                                // we have a battery (we are a tool)
        //                                // Thankfully we have this naming convention
        //                                Transform batSlot = thisItem.transform.Find("BatterySlot");
        //                                if (batSlot == null)
        //                                {
        //                                    Logger.Warn($"Failed to load modular storage item {thisItem.name} to modular storage in vehicle {mv.GetName()}.");
        //                                    continue;
        //                                }
        //                                result = new TaskResult<GameObject>();
        //                                yield return CraftData.InstantiateFromPrefabAsync(TechType.Battery, result, false);
        //                                GameObject newBat = result.Get();
        //                                if (newBat.GetComponent<Battery>() != null)
        //                                {
        //                                    newBat.GetComponent<Battery>().charge = techtype.Item2;
        //                                    Logger.Warn($"Failed to load modular storage battery {thisItem.name} to modular storage in vehicle {mv.GetName()}.");
        //                                }
        //                                newBat.transform.SetParent(batSlot);
        //                                newBat.SetActive(false);
        //                            }
        //                        }
        //                        thisItem.transform.SetParent(mv.Com.StorageRootObject.transform);
        //                        try
        //                        {
        //                            thisContainer.AddItem(thisItem.GetComponent<Pickupable>());
        //                        }
        //                        catch (Exception e)
        //                        {
        //                            Logger.LogException($"Failed to add storage item {thisItem.name} to modular storage in vehicle {mv.GetName()}.", e);
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
        internal static List<Tuple<Vector3, List<Tuple<Vector3, batteries>>>> SerializeInnateStorage()
        {
            return new List<Tuple<Vector3, List<Tuple<Vector3, batteries>>>>();
        }
        //internal static IEnumerator DeserializeInnateStorage(SaveData data, ModVehicle mv)
        //{
        //    if (data == null || mv == null || data.InnateStorages == null)
        //    {
        //        yield break;
        //    }
        //    // try to match against a saved vehicle in our list
        //    foreach (Tuple<Vector3, List<Tuple<Vector3, batteries>>> vehicle in data.InnateStorages)
        //    {
        //        if (MatchMv(mv, vehicle.Item1))
        //        {
        //            foreach (var thisStorage in vehicle.Item2)
        //            {
        //                bool isStorageMatched = false;
        //                if (mv.GetComponentsInChildren<InnateStorageContainer>().Count() == 0)
        //                {
        //                    continue;
        //                }
        //                // load up the storages
        //                foreach (var isc in mv.GetComponentsInChildren<InnateStorageContainer>())
        //                {
        //                    isStorageMatched = false;
        //                    Vector3 thisLocalPos = mv.transform.InverseTransformPoint(isc.transform.position);
        //                    if (Vector3.Distance(thisLocalPos, thisStorage.Item1) < 0.05f) // this is a weird amount of drift, but I'm afraid to use ==
        //                    {
        //                        isStorageMatched = true;
        //                        yield return UWE.CoroutineHost.StartCoroutine(LoadThisStorage(mv, thisStorage.Item2, isc));
        //                        break;
        //                    }
        //                }
        //                if (!isStorageMatched)
        //                {
        //                    Logger.Warn($"Failed to normally restore the contents of the {mv.GetName()}. Trying the old method.");
        //                    foreach (var isc in mv.GetComponentsInChildren<InnateStorageContainer>())
        //                    {
        //                        isStorageMatched = false;
        //                        if (Vector3.Distance(isc.transform.position, thisStorage.Item1) < 0.1f) // this is a weird amount of drift, but I'm afraid to use ==
        //                        {
        //                            isStorageMatched = true;
        //                            yield return UWE.CoroutineHost.StartCoroutine(LoadThisStorage(mv, thisStorage.Item2, isc));
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
        //                    Logger.Error($"Failed to restore the contents of the {mv.GetName()}.");
        //                }
        //            }
        //        }
        //    }
        //    yield break;
        //}
        internal static List<Tuple<Vector3, batteries>> SerializeBatteries()
        {
            return new List<Tuple<Vector3, batteries>>();
        }
        //internal static IEnumerator DeserializeBatteries(SaveData data, ModVehicle mv)
        //{
        //    if (data == null || mv == null || data.Batteries == null)
        //    {
        //        yield break;
        //    }
        //    // try to match against a saved vehicle in our list
        //    foreach (Tuple<Vector3, batteries> vehicle in data.Batteries)
        //    {
        //        if (MatchMv(mv, vehicle.Item1))
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
        //                    thisItem.transform.SetParent(mv.Com.StorageRootObject.transform);
        //                    mv.Com.Batteries[battery.i].BatterySlot.gameObject.GetComponent<EnergyMixin>().battery = thisItem.GetComponent<Battery>();
        //                    mv.Com.Batteries[battery.i].BatterySlot.gameObject.GetComponent<EnergyMixin>().batterySlot.AddItem(thisItem.GetComponent<Pickupable>());
        //                    thisItem.SetActive(false);
        //                }
        //                catch (Exception e)
        //                {
        //                    Logger.LogException($"Failed to load battery {thisItem.name} into vehicle : {mv.GetName()}.", e);
        //                }
        //            }
        //        }
        //    }
        //    yield break;
        //}
        internal static List<Tuple<Vector3, batteries>> SerializeBackupBatteries()
        {
            return new List<Tuple<Vector3, batteries>>();
        }
        //internal static IEnumerator DeserializeBackupBatteries(SaveData data, Submarine mv)
        //{
        //    if (data == null || mv == null || data.BackupBatteries == null)
        //    {
        //        yield break;
        //    }
        //    // try to match against a saved vehicle in our list
        //    foreach (Tuple<Vector3, batteries> slot in data.BackupBatteries)
        //    {
        //        if (MatchMv(mv, slot.Item1))
        //        {
        //            if (mv.Com.BackupBatteries.Count == 0)
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
        //                    thisItem.transform.SetParent(mv.Com.StorageRootObject.transform);
        //                    mv.Com.BackupBatteries[battery.i].BatterySlot.gameObject.GetComponent<EnergyMixin>().battery = thisItem.GetComponent<Battery>();
        //                    mv.Com.BackupBatteries[battery.i].BatterySlot.gameObject.GetComponent<EnergyMixin>().batterySlot.AddItem(thisItem.GetComponent<Pickupable>());
        //                    thisItem.SetActive(false);
        //                }
        //                catch (Exception e)
        //                {
        //                    Logger.LogException($"Failed to load battery {thisItem.name} into vehicle : {mv.GetName()}.", e);
        //                }
        //            }
        //        }
        //    }
        //    yield break;
        //}
        internal static List<Tuple<Vector3, bool>> SerializePlayerInside()
        {
            return new List<Tuple<Vector3, bool>>();
        }
        //internal static IEnumerator DeserializePlayerInside(SaveData data, ModVehicle mv)
        //{
        //    if (data == null || mv == null || data.IsPlayerInside == null)
        //    {
        //        yield break;
        //    }
        //    foreach (Tuple<Vector3, bool> vehicle in data.IsPlayerInside)
        //    {
        //        if (MatchMv(mv, vehicle.Item1) && vehicle.Item2)
        //        {
        //            try
        //            {
        //                mv.PlayerEntry();
        //            }
        //            catch (Exception e)
        //            {
        //                Logger.LogException($"Failed to load player into vehicle : {mv.GetName()}.", e);
        //            }
        //            yield break;
        //        }
        //    }
        //}
        internal static List<Tuple<Vector3, string, color, color, color, color, bool>> SerializeAesthetics()
        {
            return new List<Tuple<Vector3, string, color, color, color, color, bool>>();
        }
        //internal static IEnumerator DeserializeAesthetics(SaveData data, ModVehicle mv)
        //{
        //    if (data == null || mv == null || data.AllVehiclesAesthetics == null)
        //    {
        //        yield break;
        //    }
        //    VehicleColor SynthesizeColor(color col)
        //    {
        //        return new VehicleColor(new Color(col.Item1, col.Item2, col.Item3, col.Item4));
        //    }
        //    foreach (Tuple<Vector3, string, color, color, color, color, bool> vehicle in data.AllVehiclesAesthetics)
        //    {
        //        if (MatchMv(mv, vehicle.Item1))
        //        {
        //            try
        //            {
        //                if (mv is Submarine mvSub && mvSub.Com.ColorPicker != null)
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
        //                    mv.SetBaseColor(SynthesizeColor(vehicle.Item3));
        //                    mv.SetInteriorColor(SynthesizeColor(vehicle.Item4));
        //                    mv.SetStripeColor(SynthesizeColor(vehicle.Item5));
        //                    mv.SetNameColor(SynthesizeColor(vehicle.Item6));

        //                    mv.subName.SetColor(0, Vector3.zero, mv.BaseColor.RGB);
        //                    mv.subName.SetColor(1, Vector3.zero, mv.NameColor.RGB);
        //                    mv.subName.SetColor(2, Vector3.zero, mv.InteriorColor.RGB);
        //                    mv.subName.SetColor(3, Vector3.zero, mv.StripeColor.RGB);
        //                }
        //                break;
        //            }
        //            catch (Exception e)
        //            {
        //                Logger.LogException($"Failed to load color details for vehicle : {mv.GetName()}.", e);
        //            }
        //        }
        //    }
        //}
        internal static List<Tuple<Vector3, bool>> SerializePlayerControlling()
        {
            return new List<Tuple<Vector3, bool>>();
        }
        //internal static IEnumerator DeserializePlayerControlling(SaveData data, ModVehicle mv)
        //{
        //    if (data == null || mv == null || data.IsPlayerControlling == null)
        //    {
        //        yield break;
        //    }
        //    foreach (Tuple<Vector3, bool> vehicle in data.IsPlayerControlling)
        //    {
        //        if (MatchMv(mv, vehicle.Item1) && vehicle.Item2)
        //        {
        //            try
        //            {
        //                mv.BeginHelmControl();
        //            }
        //            catch (Exception e)
        //            {
        //                Logger.LogException($"Failed to load player into vehicle : {mv.GetName()}.", e);
        //            }
        //            yield break;
        //        }
        //    }
        //}
        internal static List<Tuple<Vector3, string>> SerializeSubName()
        {
            return new List<Tuple<Vector3, string>>();
        }
        //internal static IEnumerator DeserializeSubName(SaveData data, ModVehicle mv)
        //{
        //    if (data == null || mv == null || data.SubNames == null)
        //    {
        //        yield break;
        //    }
        //    foreach (Tuple<Vector3, string> vehicle in data.SubNames)
        //    {
        //        if (MatchMv(mv, vehicle.Item1))
        //        {
        //            try
        //            {
        //                mv.subName.SetName(vehicle.Item2);
        //            }
        //            catch (Exception e)
        //            {
        //                Logger.LogException($"Failed to load SubName for vehicle : {mv.GetName()}.", e);
        //            }
        //            yield break;
        //        }
        //    }
        //}
        internal static IEnumerator LoadThisStorage(AvsVehicle mv, batteries thisStorage, InnateStorageContainer matchedContainer)
        {
            foreach (var techtype in thisStorage)
            {
                TaskResult<GameObject> result = new TaskResult<GameObject>();
                System.String techTypeString = techtype.Item1.Replace("Undiscovered", ""); // fix for yet-"undiscovered" creature eggs
                bool resulty = TechTypeExtensions.FromString(techTypeString, out TechType thisTT, true);
                if (!resulty)
                {
                    continue;
                }
                yield return CraftData.InstantiateFromPrefabAsync(thisTT, result, false);
                GameObject thisItem = result.Get();
                if (techtype.Item2 >= 0)
                {
                    // check whether we *are* a battery xor we *have* a battery
                    if (thisItem.GetComponent<Battery>() != null && thisItem.GetComponentInChildren<Battery>() != null)
                    {
                        // we are a battery
                        thisItem.GetComponentInChildren<Battery>().charge = techtype.Item2;
                    }
                    else
                    {
                        // we have a battery (we are a tool)
                        // Thankfully we have this naming convention
                        Transform batSlot = thisItem.transform.Find("BatterySlot");
                        if (batSlot == null)
                        {
                            Logger.Warn($"Failed to load innate storage item {thisItem.name} to modular storage for {mv.name} : {mv.GetName()}.");
                            continue;
                        }
                        result = new TaskResult<GameObject>();
                        yield return CraftData.InstantiateFromPrefabAsync(TechType.Battery, result, false);
                        GameObject newBat = result.Get();
                        if (newBat.GetComponent<Battery>() != null)
                        {
                            newBat.GetComponent<Battery>().charge = techtype.Item2;
                            Logger.Warn($"Failed to load innate storage battery {thisItem.name} to modular storage for {mv.name} : {mv.GetName()}.");
                        }
                        newBat.transform.SetParent(batSlot);
                        newBat.SetActive(false);
                    }
                }
                thisItem.transform.SetParent(mv.Com.StorageRootObject.transform);
                try
                {
                    matchedContainer.Container.AddItem(thisItem.GetComponent<Pickupable>());
                }
                catch (Exception e)
                {
                    Logger.LogException($"Failed to add storage item {thisItem.name} to modular storage for {mv.name} : {mv.GetName()}.", e);
                }
                thisItem.SetActive(false);
            }



        }
    }
}
