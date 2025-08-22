using AVS.Assets;
using AVS.Log;
using AVS.Util;
using System.Collections;
using System.Collections.Generic;
using AVS.VehicleBuilding;
using UnityEngine;

namespace AVS;

/// <summary>
/// Manages the initialization and setup of upgrade slots for AVS vehicles.
/// </summary>
/// <remarks>The <see cref="UpgradeProxy"/> class is responsible for creating and managing upgrade slots
/// and assigning them to specified proxy transforms. It initializes
/// the slots during the <see cref="Awake"/> method by starting a coroutine to ensure the Seamoth is ready and then
/// setting up the upgrade slots.</remarks>
public class UpgradeProxy : MonoBehaviour
{
    /// <summary>
    /// Proxies for individual upgrade slots.
    /// Specifies where the upgrade modules will be instantiated.
    /// </summary>
    public Transform[]? proxies;

    /// <summary>
    /// Slot list passed on to the VehicleUpgradeConsoleInput.
    /// </summary>
    public List<VehicleUpgradeConsoleInput.Slot>? slots = null;

    /// <inheritdoc />
    public void Awake()
    {
        MainPatcher.Instance.StartCoroutine(GetSeamothBitsASAP());
    }

    /// <summary>
    /// Initializes and configures the upgrade slots as soon as possible.
    /// </summary>
    /// <remarks>This method ensures that the Seamoth is available and then sets up the upgrade slots
    /// by instantiating the necessary models. It clears any existing proxies and assigns new models to each slot
    /// based on the current configuration.</remarks>
    /// <returns>An enumerator that can be used to iterate through the coroutine execution process.</returns>
    public IEnumerator GetSeamothBitsASAP()
    {
        var log = LogWriter.Default.Tag(nameof(UpgradeProxy));

        log.Write("Waiting for Seamoth to be ready...");
        yield return SeamothHelper.WaitUntilLoaded();

        log.Write("Seamoth is ready, setting up upgrade slots...");

        slots = [];
        var module = SeamothHelper.RequireSeamoth.transform
            .Find("Model/Submersible_SeaMoth/Submersible_seaMoth_geo/engine_console_key_02_geo").gameObject;
        if (module == null)
        {
            log.Error("Could not find the upgrade module in the Seamoth prefab.");
            yield break;
        }

        if (proxies == null)
        {
            log.Error("Proxies array is null. Cannot proceed with upgrade slot setup.");
            yield break;
        }

        log.Write($"Found upgrade module: {module.NiceName()}. Processing {proxies.Length} proxies");
        for (var i = 0; i < proxies.Length; i++)
        {
            proxies[i].DestroyChildren();
            var model = Instantiate(module, proxies[i]);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = new Vector3(100, 100, 100);
            LogWriter.Default.Write(
                $"Instantiating upgrade module #{i}/{proxies.Length} in {proxies[i].NiceName()}: {model.NiceName()} using scale {model.transform.localScale}");
            VehicleUpgradeConsoleInput.Slot slot;
            slot.id = ModuleBuilder.ModuleName(i);
            slot.model = model;
            slots.Add(slot);
        }

        LogWriter.Default.Write($"UpgradeProxy: Created {slots.Count} upgrade slots.");
        GetComponentInChildren<VehicleUpgradeConsoleInput>().slots = slots.ToArray();
    }
}