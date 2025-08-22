using UnityEngine;

namespace AVS.VehicleComponents;

internal class DebugBatteryEnergyMixin : EnergyMixin
{
    [SerializeField] internal Transform? originalProxy;
    //public override void NotifyHasBattery(InventoryItem item)
    //{
    //    LogWriter.Default.Write($"DebugBatteryEnergyMixin: NotifyHasBattery called");
    //    try
    //    {
    //        foreach (var model in batteryModels)
    //            if (model.model.IsNotNull())
    //                LogWriter.Default.Write($"Logging battery model: {model.model.NiceName()} active:={model.model.activeInHierarchy}, scale:={model.model.transform.localScale}");
    //    }
    //    catch (System.Exception e)
    //    {
    //        LogWriter.Default.Error($"Error logging battery models in {transform.NiceName()}: {e.Message}");
    //    }
    //    try
    //    {
    //        if (controlledObjects.IsNotNull())
    //            foreach (var obj in controlledObjects)
    //                if (obj.IsNotNull())
    //                    LogWriter.Default.Write($"Logging controlled object: {obj.NiceName()} active:={obj.activeInHierarchy}");
    //    }
    //    catch (System.Exception e)
    //    {
    //        LogWriter.Default.Error($"Error logging controlled objects in {transform.NiceName()}: {e.Message}");
    //    }
    //    try
    //    {
    //        if (originalProxy.IsNotNull())
    //        {
    //            LogWriter.Default.Write($"Logging original proxy: {originalProxy.NiceName()} active:={originalProxy.gameObject.activeInHierarchy}, scale:={originalProxy.localScale}");
    //            foreach (Transform child in originalProxy)
    //                LogWriter.Default.Write($"Logging child in proxy: {child.NiceName()} active:={child.gameObject.activeInHierarchy}, scale:={child.localScale}");
    //        }
    //    }
    //    catch (System.Exception e)
    //    {
    //        LogWriter.Default.Error($"Error logging original proxy children in {transform.NiceName()}: {e.Message}");
    //    }

    //    base.NotifyHasBattery(item);

    //    try
    //    {
    //        foreach (var model in batteryModels)
    //            if (model.model.IsNotNull())
    //                LogWriter.Default.Write($"Notified: Logging battery model: {model.model.NiceName()} active:={model.model.activeInHierarchy}, scale:={model.model.transform.localScale}");
    //    }
    //    catch (System.Exception e)
    //    {
    //        LogWriter.Default.Error($"Notified: Error logging battery models in {transform.NiceName()}: {e.Message}");
    //    }
    //    try
    //    {
    //        if (controlledObjects.IsNotNull())
    //            foreach (var obj in controlledObjects)
    //                if (obj.IsNotNull())
    //                    LogWriter.Default.Write($"Notified: Logging controlled object: {obj.NiceName()} active:={obj.activeInHierarchy}");
    //    }
    //    catch (System.Exception e)
    //    {
    //        LogWriter.Default.Error($"Notified: Error logging controlled objects in {transform.NiceName()}: {e.Message}");
    //    }
    //    try
    //    {
    //        if (originalProxy.IsNotNull())
    //        {
    //            LogWriter.Default.Write($"Logging original proxy: {originalProxy.NiceName()} active:={originalProxy.gameObject.activeInHierarchy}, scale:={originalProxy.localScale}");

    //            foreach (Transform child in originalProxy)
    //                LogWriter.Default.Write($"Notified: Logging child in proxy: {child.NiceName()} active:={child.gameObject.activeInHierarchy}, scale:={child.localScale}");
    //        }
    //    }
    //    catch (System.Exception e)
    //    {
    //        LogWriter.Default.Error($"Notified: Error logging original proxy children in {transform.NiceName()}: {e.Message}");
    //    }

    //}
}