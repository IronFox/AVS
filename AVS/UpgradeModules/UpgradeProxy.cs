using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AVS
{
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
        public IReadOnlyList<Transform> proxies = new List<Transform>();
        /// <summary>
        /// Slot list passed on to the VehicleUpgradeConsoleInput.
        /// </summary>
        public List<VehicleUpgradeConsoleInput.Slot>? slots = null;

        /// <inheritdoc />
        public void Awake()
        {
            UWE.CoroutineHost.StartCoroutine(GetSeamothBitsASAP());
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
            yield return UWE.CoroutineHost.StartCoroutine(SeamothHelper.EnsureSeamoth());

            slots = new List<VehicleUpgradeConsoleInput.Slot>();
            var module = SeamothHelper.Seamoth!.transform.Find("Model/Submersible_SeaMoth/Submersible_seaMoth_geo/engine_console_key_02_geo").gameObject;
            for (int i = 0; i < proxies.Count; i++)
            {
                foreach (Transform tran in proxies[i])
                {
                    Destroy(tran.gameObject);
                }
                GameObject model = GameObject.Instantiate(module, proxies[i]);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
                VehicleUpgradeConsoleInput.Slot slot;
                slot.id = ModuleBuilder.ModuleName(i);
                slot.model = model;
                slots.Add(slot);
            }
            GetComponentInChildren<VehicleUpgradeConsoleInput>().slots = slots.ToArray();
        }

    }
}
