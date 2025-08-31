using AVS.BaseVehicle;
using AVS.Log;
using AVS.Util;
using UnityEngine;

namespace AVS.VehicleComponents
{
    /// <summary>
    /// Common parent class for components that are attached to an AVS vehicle.
    /// </summary>
    public abstract class AvAttached : MonoBehaviour
    {
        [SerializeField]
        internal AvsVehicle? av;

        /// <summary>
        /// Queries the vehicle this component is attached to. If not already set, it will search the parent hierarchy for an <see cref="AvsVehicle"/> component.
        /// </summary>
        protected AvsVehicle AV
        {
            get
            {
                if (av.IsNull())
                    av = GetComponentInParent<AvsVehicle>();
                return av.OrThrow($"{GetType().Name} does not have a vehicle attached and none could be retrieved from hierarchy. This should not happen.");

            }
        }

        internal static T Ensure<T>(GameObject go, AvsVehicle av, SmartLog log) where T : AvAttached
        {
            var comp = EnsureSelfDestructing<T>(go, av, log);
            return comp.OrThrow($"Failed to add required component {typeof(T).Name} to {av.NiceName()}");
        }
        internal static T Ensure<T>(AvsVehicle av, SmartLog log) where T : AvAttached
        {
            var comp = EnsureSelfDestructing<T>(av, log);
            return comp.OrThrow($"Failed to add required component {typeof(T).Name} to {av.NiceName()}");
        }

        /// <summary>
        /// Ensures that the specified component of type <typeparamref name="T"/> exists on the given vehicle's
        /// GameObject. If the component does not exist, it is added and initialized.
        /// </summary>
        /// <remarks>This variant allows components to instantly self-destruct upon instantiation, returning null in this case.</remarks>
        /// <typeparam name="T">The type of the component to ensure. Must inherit from <see cref="AvAttached"/>.</typeparam>
        /// <param name="av">The vehicle instance to be attached to the created component</param>
        /// <param name="log">The logging instance used to record actions performed by this method</param>
        /// <param name="go">The GameObject to which the component should be attached</param>
        /// <returns>The existing or newly added component of type <typeparamref name="T"/>, or <c>null</c> if the component
        /// could not be added.</returns>
        internal static T? EnsureSelfDestructing<T>(GameObject go, AvsVehicle av, SmartLog log) where T : AvAttached
        {
            var comp = go.GetComponent<T>();
            if (comp.IsNotNull())
                return comp;
            log.Write($"Adding component {typeof(T).Name} to {go.NiceName()}");
            comp = go.AddComponent<T>();
            if (comp.IsNull())
            {
                log.Write($"Added component {typeof(T).Name} instantly self-destructed");
                return null;
            }
            comp.av = av;
            return comp;
        }
        /// <summary>
        /// Ensures that the specified component of type <typeparamref name="T"/> exists on the given vehicle's
        /// GameObject. If the component does not exist, it is added and initialized.
        /// </summary>
        /// <remarks>This variant allows components to instantly self-destruct upon instantiation, returning null in this case.</remarks>
        /// <typeparam name="T">The type of the component to ensure. Must inherit from <see cref="AvAttached"/>.</typeparam>
        /// <param name="av">The vehicle instance whose GameObject is being checked or modified</param>
        /// <param name="log">The logging instance used to record actions performed by this method</param>
        /// <returns>The existing or newly added component of type <typeparamref name="T"/>, or <c>null</c> if the component
        /// could not be added.</returns>
        internal static T? EnsureSelfDestructing<T>(AvsVehicle av, SmartLog log) where T : AvAttached
            => EnsureSelfDestructing<T>(av.gameObject, av, log);
    }
}
