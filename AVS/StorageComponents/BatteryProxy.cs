using AVS.Assets;
using AVS.Log;
using AVS.Util;
using AVS.VehicleComponents;
using System.Collections;
using UnityEngine;

namespace AVS.StorageComponents;

internal class BatteryProxy : AvAttached
{
    public Transform? proxy = null;
    public EnergyMixin? mixin = null;

    public void Awake()
    {
        AV.Owner.StartAvsCoroutine(
            nameof(BatteryProxy) + '.' + nameof(GetSeamothBitsASAP),
            GetSeamothBitsASAP);
    }

    public IEnumerator GetSeamothBitsASAP(SmartLog log)
    {
        if (proxy is null || mixin is null)
        {
            log.Error($"BatteryProxy in {AV.NiceName()} has not properly configured {this.NiceName()}");
            // reload racing condition ?
            // no...
            yield break;
        }
        var owner = av!.Owner;
        //var seamothLoader = PrefabLoader.Request(TechType.Seamoth);
        yield return SeamothHelper.WaitUntilLoaded();
        log.Debug($"Seamoth prefab loaded for {this.NiceName()} in {owner.NiceName()}");
        var seamothEnergyMixin = SeamothHelper.RequireSeamoth.GetComponent<EnergyMixin>();
        mixin.batteryModels = new EnergyMixin.BatteryModels[seamothEnergyMixin.batteryModels.Length];
        for (var i = 0; i < seamothEnergyMixin.batteryModels.Length; i++)
        {
            var but = seamothEnergyMixin.batteryModels[i];
            log.Debug($"Cloning battery model #{i}/{seamothEnergyMixin.batteryModels.Length} {but.techType.AsString()} for {this.NiceName()} in {owner.NiceName()}");
            var mod = new EnergyMixin.BatteryModels
            {
                model = Instantiate(but.model),
                techType = but.techType
            };
            mixin.batteryModels[i] = mod;
        }
        log.Debug($"Cloned {mixin.batteryModels.Length} battery models for {this.NiceName()} in {owner.NiceName()}");
        //LogWriter.Default.Write($"Destroying {proxy.childCount} child(ren) in {proxy.NiceName()}");
        log.Debug($"Destroying {proxy.childCount} child(ren) in {proxy.NiceName()}");
        foreach (Transform tran in proxy)
        {
            tran.parent = null; // detach from parent
            Destroy(tran.gameObject);
        }

        log.Debug($"Instantiating {mixin.batteryModels.Length} battery models in {proxy.NiceName()}");
        for (var i = 0; i < mixin.batteryModels.Length; i++)
        {
            mixin.batteryModels[i].model.SetActive(true);
            //LogWriter.Default.Write($"Instantiating battery model #{i}/{mixin.batteryModels.Length} {mixin.batteryModels[i].techType.AsString()} in {proxy.NiceName()}");
            var model = Instantiate(mixin.batteryModels[i].model, proxy);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            if (!model.name.ToLower().Contains("ion"))
                model.transform.localScale *= 100f;
            mixin.batteryModels[i].model = model;
        }
        log.Debug($"Done");
    }
}