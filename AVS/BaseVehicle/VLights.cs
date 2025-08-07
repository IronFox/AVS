using AVS.Util;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AVS.BaseVehicle
{
    public abstract partial class AvsVehicle
    {

        /// <summary>
        /// Use <see cref="LightsOffSound"/> instead.
        /// </summary>
        /// <remarks> Prefabrication fields must remain open fields or
        /// Unity instantiation will not preserve them.
        /// Since the vehicle has multiple custom emitters, we cannot
        /// fetch it during Awake()</remarks>
        [SerializeField]
        private FMOD_CustomEmitter? lightsOffSound;

        /// <summary>
        /// Use <see cref="LightsOnSound"/> instead.
        /// </summary>
        /// <remarks> Prefabrication fields must remain open fields or
        /// Unity instantiation will not preserve them.
        /// Since the vehicle has multiple custom emitters, we cannot
        /// fetch it during Awake()</remarks>
        [SerializeField]
        private FMOD_CustomEmitter? lightsOnSound;

        /// <summary>
        /// Sound to play when the vehicle lights are turned on.
        /// Set during prefabrication.
        /// </summary>
        public FMOD_CustomEmitter LightsOffSound
            => lightsOffSound.OrThrow(
                () => new InvalidOperationException(
                    $"Trying to access LightsOffSound but the prefabrication did not assign this field"));


        /// <summary>
        /// Sound to play when the vehicle lights are turned off.
        /// Set during prefabrication.
        /// </summary>
        public FMOD_CustomEmitter LightsOnSound
            => lightsOnSound.OrThrow(
                () => new InvalidOperationException(
                    $"Trying to access LightsOnSound but the prefabrication did not assign this field"));

        /// <summary>
        /// Populated during prefabrication.
        /// </summary>
        [SerializeField]
        internal List<GameObject> volumetricLights = new List<GameObject>();


        /// <summary>
        /// The headlights controller for this vehicle.
        /// Set during Awake().
        /// </summary>
        public HeadLightsController? HeadlightsController { get; private set; }  //set during awake()

    }
}
