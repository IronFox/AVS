using AVS.Crafting;
using AVS.Localization;
using AVS.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AVS.Admin;

internal class ConsoleCommands : MonoBehaviour
{
    internal static bool isUndockConsoleCommand = false; // hacky

    public void Awake()
    {
        //DevConsole.RegisterConsoleCommand(this, "vfhelp", false, false);
        //DevConsole.RegisterConsoleCommand(this, "givevfupgrades", false, false);
        //DevConsole.RegisterConsoleCommand(this, "givevfseamothupgrades", false, false);
        //DevConsole.RegisterConsoleCommand(this, "givevfprawnupgrades", false, false);
        //DevConsole.RegisterConsoleCommand(this, "givevfcyclopsupgrades", false, false);
        //DevConsole.RegisterConsoleCommand(this, "logvfupgrades", false, false);
        //DevConsole.RegisterConsoleCommand(this, "logvfvehicles", false, false);
        //DevConsole.RegisterConsoleCommand(this, "logvfvoices", false, false);
        //DevConsole.RegisterConsoleCommand(this, "vfspawncodes", false, false);
        //DevConsole.RegisterConsoleCommand(this, "undockclosest", false, false);
        //DevConsole.RegisterConsoleCommand(this, "vfdestroy", false, false);
    }

    public void OnConsoleCommand_vfhelp(NotificationCenter.Notification _)
    {
        //Logger.PDANote("givevfupgrades");
        //Logger.PDANote("givevfseamothupgrades");
        //Logger.PDANote("givevfprawnupgrades");
        //Logger.PDANote("givevfcyclopsupgrades");
        //Logger.PDANote("logvfupgrades");
        //Logger.PDANote("logvfvehicles");
        //Logger.PDANote("logvfvoices");
        //Logger.PDANote("vfspawncodes");
        //Logger.PDANote("undockclosest");
        //Logger.PDANote("vfdestroy [vehicle type]");
    }

    public void OnConsoleCommand_givevfupgrades(NotificationCenter.Notification _)
    {
        UpgradeRegistrar.UpgradeIcons
            .Select(x => x.Key)
            .Where(x => UWE.Utils.TryParseEnum<TechType>(x, out var techType))
            .ForEach(x => DevConsole.instance.Submit("item " + x));
    }

    public void OnConsoleCommand_givevfseamothupgrades(NotificationCenter.Notification _)
    {
        UpgradeRegistrar.UpgradeIcons
            .Select(x => x.Key + "Seamoth")
            .Where(x => UWE.Utils.TryParseEnum<TechType>(x, out var techType))
            .ForEach(x => DevConsole.instance.Submit("item " + x));
    }

    public void OnConsoleCommand_givevfprawnupgrades(NotificationCenter.Notification _)
    {
        UpgradeRegistrar.UpgradeIcons
            .Select(x => x.Key + "Exosuit")
            .Where(x => UWE.Utils.TryParseEnum<TechType>(x, out var techType))
            .ForEach(x => DevConsole.instance.Submit("item " + x));
    }

    public void OnConsoleCommand_givevfcyclopsupgrades(NotificationCenter.Notification _)
    {
        UpgradeRegistrar.UpgradeIcons
            .Select(x => x.Key + "Cyclops")
            .Where(x => UWE.Utils.TryParseEnum<TechType>(x, out var techType))
            .ForEach(x => DevConsole.instance.Submit("item " + x));
    }

    public void OnConsoleCommand_logvfupgrades(NotificationCenter.Notification _)
    {
        UpgradeRegistrar.UpgradeIcons.Select(x => x.Key).ForEach(x => Logger.Log(x));
    }

    public void OnConsoleCommand_logvfvehicles(NotificationCenter.Notification _)
    {
        AvsVehicleManager.VehicleTypes.Select(x => x.TechType).ForEach(x => Logger.Log(x.AsString()));
    }

    //public void OnConsoleCommand_logvfvoices(NotificationCenter.Notification _)
    //{
    //	VoiceManager.LogAllAvailableVoices();
    //}
    //public void OnConsoleCommand_vfspawncodes(NotificationCenter.Notification _)
    //{
    //    MainPatcher.Instance.StartCoroutine(ListSpawnCodes());
    //}
    private static IEnumerator ListSpawnCodes()
    {
        var allCodes = new List<string>();
        allCodes.AddRange(AvsVehicleManager.VehicleTypes.Select(x => x.TechType.AsString()));
        allCodes.AddRange(UpgradeRegistrar.UpgradeIcons.Select(x => x.Key));
        foreach (var code in allCodes)
        {
            Logger.PDANote(code, 4f);
            yield return new WaitForSeconds(0.3f);
        }
    }

    public void OnConsoleCommand_undockclosest(NotificationCenter.Notification _)
    {
        void MaybeUndock(VehicleDockingBay dock)
        {
            if (dock.dockedVehicle.IsNotNull())
            {
                Logger.PDANote(
                    $"{Translator.Get(TranslationKey.Command_UndockingVehicle)} {dock.dockedVehicle.GetName()}");
                isUndockConsoleCommand = true;
                dock.dockedVehicle.Undock();
                isUndockConsoleCommand = false;
            }
            else
            {
                Logger.PDANote(Translator.Get(TranslationKey.Command_NothingToUndock));
            }
        }

        var distanceToPlayer = float.PositiveInfinity;
        VehicleDockingBay? closestBay = null;
        foreach (var marty in Patches.VehicleDockingBayPatch.DockingBays.Where(x => x.IsNotNull()))
        {
            var thisDistance = Vector3.Distance(Player.main.transform.position, marty.transform.position);
            if (thisDistance < distanceToPlayer)
            {
                closestBay = marty;
                distanceToPlayer = thisDistance;
            }
        }

        if (closestBay.IsNotNull())
            MaybeUndock(closestBay);
        else
            Logger.PDANote(Translator.Get(TranslationKey.Command_NothingToUndock));
    }

    public void OnConsoleCommand_vfdestroy(NotificationCenter.Notification notif)
    {
        if (notif.data.IsNull() || notif.data.Count == 0)
            ErrorMessage.AddError("vfdestroy error: no vehicle type specified. Ex: \"vfdestroy exosuit\"");
        var vehicleType = notif.data?[0] as string;
        ErrorMessage.AddWarning($"vfdestroy doing destroy on {vehicleType}");
        var found = GameObjectManager<Vehicle>.FindNearestSuch(Player.main.transform.position,
            x => x.name.Equals($"{vehicleType}(Clone)", System.StringComparison.OrdinalIgnoreCase));
        if (found.IsNull())
        {
            ErrorMessage.AddWarning($"vfdestroy found no vehicle matching \"{vehicleType}\"");
            var nearest = GameObjectManager<Vehicle>.FindNearestSuch(Player.main.transform.position);
            if (nearest.IsNotNull())
                ErrorMessage.AddWarning(
                    $"Did you mean \"vfdestroy {nearest.name.Substring(0, nearest.name.Length - "(Clone)".Length)}\"?"
                        .ToLower());
            return;
        }

        ErrorMessage.AddWarning($"Destroying {found.name}");
        DestroyImmediate(found.gameObject);
    }
}