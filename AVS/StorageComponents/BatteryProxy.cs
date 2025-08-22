using AVS.Assets;
using System.Collections;
using UnityEngine;

namespace AVS.StorageComponents;

internal class BatteryProxy : MonoBehaviour
{
    public Transform? proxy = null;
    public EnergyMixin? mixin = null;

    public void Awake()
    {
        MainPatcher.Instance.StartCoroutine(GetSeamothBitsASAP());
    }

    public IEnumerator GetSeamothBitsASAP()
    {
        if (proxy is null || mixin is null)
            // reload reload condition ?
            // no...
            yield break;
        //var seamothLoader = PrefabLoader.Request(TechType.Seamoth);
        var seamothEnergyMixin = SeamothHelper.RequireSeamoth.GetComponent<EnergyMixin>();
        mixin.batteryModels = new EnergyMixin.BatteryModels[seamothEnergyMixin.batteryModels.Length];
        for (var i = 0; i < seamothEnergyMixin.batteryModels.Length; i++)
        {
            var but = seamothEnergyMixin.batteryModels[i];
            var mod = new EnergyMixin.BatteryModels
            {
                model = Instantiate(but.model),
                techType = but.techType
            };
            mixin.batteryModels[i] = mod;
        }

        //LogWriter.Default.Write($"Destroying {proxy.childCount} child(ren) in {proxy.NiceName()}");
        foreach (Transform tran in proxy)
        {
            tran.parent = null; // detach from parent
            Destroy(tran.gameObject);
        }

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
        //foreach (Transform tran in proxy)
        //    LogWriter.Default.Write($"BatteryProxy child: {tran.NiceName()} in {proxy.NiceName()}");
    }
    /*
    public void Awake()
    {
        if (battery is null)
        {
            if (proxy.childCount == 0)
            {
                battery = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                battery.transform.SetParent(proxy);
                battery.transform.localScale = Vector3.one * 0.001f;
                battery.transform.localPosition = Vector3.zero;
                battery.transform.localRotation = Quaternion.identity;
            }
            else
            {
                battery = proxy.GetChild(0).gameObject;
            }
        }
    }
    public void Register()
    {
        for (int i = 0; i < mixin.batteryModels.Length; i++)
        {
            var model = GameObject.Instantiate(mixin.batteryModels[i].model, proxy);
            battery.transform.localPosition = Vector3.zero;
            battery.transform.localRotation = Quaternion.identity;
            mixin.batteryModels[i].model = model;
        }
    }
    public void ShowBattery()
    {
        battery.SetActive(true);
    }
    public void HideBattery()
    {
        battery.SetActive(false);
    }
    */
}