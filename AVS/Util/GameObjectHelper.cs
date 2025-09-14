using AVS.Log;
using AVS.SaveLoad;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AVS.Util;

/// <summary>
/// Various utility extensions and methods for querying or manipulating GameObjects and Components.
/// </summary>
public static class GameObjectHelper
{
    /// <summary>
    /// Duplicates a source component onto another object, copying all its fields in the process.
    /// </summary>
    /// <typeparam name="T">Type being copied</typeparam>
    /// <param name="original">Original component. May be null</param>
    /// <param name="destination">Destination owner</param>
    /// <returns>Duplicated component</returns>
    public static T? TryCopyComponentWithFieldsTo<T>(this T? original, GameObject destination) where T : Component
    {
        if (original.IsNull())
        {
            Logger.Error($"Original component of type {typeof(T).Name} is null, cannot copy.");
            return null;
        }

        return CopyComponentWithFieldsTo(original, destination);
    }

    /// <summary>
    /// Duplicates a source component onto another object, copying all its fields in the process.
    /// </summary>
    /// <typeparam name="T">Type being copied</typeparam>
    /// <param name="original">Original component</param>
    /// <param name="destination">Destination owner</param>
    /// <returns>Duplicated component</returns>
    public static T CopyComponentWithFieldsTo<T>(this T original, GameObject destination) where T : Component
    {
        //if (original .IsNull())
        //{
        //    Logger.Error($"Original component of type {typeof(T).Name} is null, cannot copy.");
        //    return null;
        //}
        var type = original.GetType();
        var copy = (T)destination.EnsureComponent(type);
        var fields = type.GetFields();
        foreach (var field in fields) field.SetValue(copy, field.GetValue(original));
        return copy;
    }

    /// <summary>
    /// Returns the first non-null object from the two provided.
    /// </summary>
    /// <typeparam name="T">Type being compared</typeparam>
    /// <param name="a">First object to return if not null</param>
    /// <param name="b">Second objec to return if <paramref name="a"/> is null</param>
    /// <returns><paramref name="a"/> if not null, <paramref name="b"/> if <paramref name="a"/> is null,
    /// null if both are null</returns>
    public static T? Or<T>(this T? a, T? b) where T : Object
    {
        if (a.IsNotNull())
            return a;
        return b;
    }

    /// <summary>
    /// Returns the first non-null object from the two provided.
    /// </summary>
    /// <typeparam name="T">Type being compared</typeparam>
    /// <param name="a">First object to return if not null</param>
    /// <param name="bFactory">Factory for the second object to return if <paramref name="a"/> is null</param>
    /// <returns><paramref name="a"/> if not null, otherwise the result of <paramref name="bFactory"/>.</returns>
    public static T? Or<T>(this T? a, Func<T?> bFactory) where T : Object
    {
        if (a.IsNotNull())
            return a;
        return bFactory();
    }

    /// <summary>
    /// Returns the first non-null object from the two provided.
    /// Either <paramref name="a"/> or <paramref name="b"/> must not be null.
    /// </summary>
    /// <typeparam name="T">Type being compared</typeparam>
    /// <param name="a">First object to return if not null</param>
    /// <param name="b">Second object to return if <paramref name="a"/> is null</param>. Must not be null
    /// <returns><paramref name="a"/> if not null, <paramref name="b"/> if <paramref name="a"/> is null</returns>
    public static T OrRequired<T>(this T? a, T? b) where T : Object
    {
        if (a.IsNotNull())
            return a;
        if (b.IsNotNull())
            return b;
        throw new ArgumentNullException($"Both objects are null. Cannot return a valid {typeof(T).Name} object.");
    }

    /// <summary>
    /// Returns the first non-null object from the two provided.
    /// Either <paramref name="a"/> or the result of <paramref name="bFactory"/> must not be null.
    /// </summary>
    /// <typeparam name="T">Type being compared</typeparam>
    /// <param name="a">First object to return if not null</param>
    /// <param name="bFactory">Factory for the second object to return if <paramref name="a"/> is null</param>. Must not produce null
    /// <returns><paramref name="a"/> if not null, <paramref name="bFactory"/>() if <paramref name="a"/> is null</returns>
    public static T OrRequired<T>(this T? a, Func<T?> bFactory) where T : Object
    {
        if (a.IsNotNull())
            return a;
        var b = bFactory();
        if (b.IsNotNull())
            return b;
        throw new ArgumentNullException($"Both objects are null. Cannot return a valid {typeof(T).Name} object.");
    }

    /// <summary>
    /// Returns a non-null object or throws an exception if the object is null.
    /// </summary>
    /// <typeparam name="T">Unity object type to check</typeparam>
    /// <param name="item">Unity object to check</param>
    /// <param name="exceptionFactory">Factory that produces the exception to throw. Can throw itself</param>
    /// <returns>Non-null <paramref name="item"/></returns>
    public static T OrThrow<T>(this T? item, Func<Exception> exceptionFactory) where T : Object
    {
        if (item.IsNotNull())
            return item;
        throw exceptionFactory();
    }

    /// <summary>
    /// Returns a non-null object or throws an exception if the object is null.
    /// </summary>
    /// <typeparam name="T">Unity object type to check</typeparam>
    /// <param name="item">Unity object to check</param>
    /// <param name="msg">Message to throw as an <see cref="InvalidOperationException" /></param>
    /// <returns>Non-null <paramref name="item"/></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static T OrThrow<T>(this T? item, string msg) where T : Object
    {
        if (item.IsNotNull())
            return item;
        throw new InvalidOperationException(msg);
    }

    /// <summary>
    /// Changes the active state of a GameObject and logs the action, including any exceptions that occur.
    /// </summary>
    /// <remarks>
    /// Does nothing if the object already matches the new state
    /// </remarks>
    /// <param name="gameObject">Game object being manipulated</param>
    /// <param name="toEnabled">New enabled state</param>
    /// <param name="log">Out logging facilities</param>
    public static void LoggedSetActive(this GameObject gameObject, bool toEnabled, SmartLog log)
    {
        if (!gameObject)
        {
            log.Error("GameObject is null, cannot set active state.");
            return;
        }

        try
        {
            if (gameObject.activeSelf != toEnabled)
            {
                //log.Debug($"Setting active state of {gameObject.NiceName()} to {toEnabled}");
                gameObject.SetActive(toEnabled);
                if (gameObject.activeSelf != toEnabled)
                    log.Warn($"Failed to set state of {gameObject.NiceName()} to {toEnabled}");
                //log.Debug($"Set active state of {gameObject.NiceName()} to {toEnabled}");
            }
        }
        catch (Exception e)
        {
            log.Error($"Failed to set active state of {gameObject.NiceName()}: {e.Message}", e);
        }
    }

    /// <summary>
    /// Selectively returns the transform of a GameObject.
    /// Returns null if the GameObject is null.
    /// </summary>
    public static Transform? SafeGetTransform(this GameObject? gameObject)
    {
        if (gameObject.IsNull())
            return null;
        return gameObject.transform;
    }

    /// <summary>
    /// Selectively returns the transform of a Component.
    /// Returns null if the Component is null.
    /// </summary>
    public static Transform? GetTransform(this Component? component)
    {
        if (component.IsNull())
            return null;
        return component.transform;
    }

    /// <summary>
    /// Selectively returns the GameObject of a Component.
    /// Returns null if the Component is null.
    /// </summary>
    public static GameObject? SafeGetGameObject(this Component? component)
    {
        if (component.IsNull())
            return null;
        return component.gameObject;
    }

    /// <summary>
    /// Selectively returns the transform of a Component.
    /// Returns null if the Component is null.
    /// </summary>
    public static Transform? SafeGetTransform(this Component? component)
    {
        if (component.IsNull())
            return null;
        return component.transform;
    }

    /// <summary>
    /// Selectively returns the Texture2D of a Sprite.
    /// Returns null if the Sprite is null.
    /// </summary>
    public static Texture2D? SafeGetTexture2D(this Sprite? sprite)
    {
        if (sprite.IsNull())
            return null;
        return sprite.texture;
    }

    /// <summary>
    /// Queries a nicer representation of an Object for logging purposes.
    /// Includes the object's name, type, and instance ID.
    /// Returns "&lt;null&gt;" if the object is null.
    /// </summary>
    public static string NiceName(this Object? o)
    {
        if (o.IsNull())
            return "<null>";

        var text = SanitizeObjectName(o.name);

        return $"<{o.GetType().Name}> '{text}' [{o.GetInstanceID()}]";
    }


    /// <summary>
    /// Removes any content following the first opening parenthesis in the specified string,  and returns the sanitized
    /// object name. If the input is null or empty, returns "&lt;none&gt;".
    /// </summary>
    /// <param name="text">The string to sanitize. Can be null or empty.</param>
    /// <returns>The sanitized object name with content after the first opening parenthesis removed,  or "&lt;none&gt;" if the
    /// input is null or empty.</returns>
    public static string SanitizeObjectName(this string? text)
    {
        if (text.IsNullOrEmpty())
            return "<none>";
        var num = text.IndexOf('(');
        if (num >= 0)
            text = text.Substring(0, num);
        return text;
    }

    /// <summary>
    /// Produces the full hierarchy path of a Transform as a single string using / as separator.
    /// Returns "&lt;null&gt;" if the Transform is null.
    /// </summary>
    public static string PathToString(this Component? c, Transform? root = null)
    {
        if (c.IsNull())
            return "<null>";
        var t = c.transform;
        var list = new List<string>();
        try
        {
            while (t && t != root)
            {
                var name = SanitizeObjectName(t.name);
                list.Add($"{name}[{t.GetInstanceID()}]");
                t = t.parent;
            }
        }
        catch (UnityException)
        {
        }

        list.Reverse();
        return string.Join("/", list);
    }

    /// <summary>
    /// Queries all children of a Transform as an <see cref="IEnumerable{T}" /> of Transforms.
    /// Returns an empty enumerable if the Transform is null or has no children.
    /// </summary>
    public static IEnumerable<Transform> SafeGetChildren(this Transform? transform)
    {
        if (transform.IsNull()) yield break;
        for (var i = 0; i < transform.childCount; i++) yield return transform.GetChild(i);
    }

    /// <summary>
    /// Gets the GameObject associated with a Collider.
    /// Favors the attached Rigidbody if available, otherwise uses the Collider's GameObject.
    /// Returns null if the Collider is null.
    /// </summary>
    public static GameObject? SafeGetGameObjectOf(Collider? collider)
    {
        if (collider.IsNull())
            return null;
        if (collider.attachedRigidbody) return collider.attachedRigidbody.gameObject;

        return collider.gameObject;
    }

    /// <summary>
    /// Changes the active state of a MonoBehaviour and its parent hierarchy if necessary,
    /// such that the MonoBehaviour ends up active and enabled.
    /// Logs changes and errors as errors.
    /// </summary>
    /// <param name="c">Behavior to change the state of</param>
    /// <param name="rootTransform">Hierarchy root which will not be altered. If encountered, the loop stops</param>
    public static void RequireActive(this MonoBehaviour c, Transform rootTransform)
    {
        if (!c)
        {
            Logger.Error("MonoBehaviour is null, cannot ensure active state.");
            return;
        }

        if (c.isActiveAndEnabled)
            return;

        if (!c.enabled)
        {
            Logger.Error($"{c} has been disabled. Re-enabling");
            c.enabled = true;
        }

        if (c.isActiveAndEnabled)
            return;

        var transform = c.transform;
        while (transform && transform != rootTransform)
        {
            if (!transform.gameObject.activeSelf)
            {
                Logger.Error($"{transform.gameObject} has been deactivate. Re-activating");
                transform.gameObject.SetActive(false);
                if (c.isActiveAndEnabled)
                    return;
            }

            transform = transform.parent;
        }

        if (!rootTransform.gameObject.activeSelf)
        {
            Logger.Error($"{rootTransform.gameObject} has been deactivate. Re-activating");
            rootTransform.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Retrieves all components of type <typeparamref name="T"/> from the current component and its children.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="c">The component from which to begin the search. If <see langword="null"/>, an empty array is returned.</param>
    /// <param name="includeInactive">Also include inactive game objects</param>
    /// <returns>An array of components of type <typeparamref name="T"/> found in the current component and its children.
    /// Returns an empty array if <paramref name="c"/> is <see langword="null"/> or no components of the specified
    /// type are found.</returns>
    public static T[] SafeGetComponentsInChildren<T>(this Component? c, bool includeInactive) where T : Component
    {
        if (c.IsNull())
            return [];
        return c.GetComponentsInChildren<T>(includeInactive);
    }

    /// <summary>
    /// Retrieves all components of type <typeparamref name="T"/> from the specified <see cref="GameObject"/>  and
    /// its child objects. Returns an empty array if the <see cref="GameObject"/> is null.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve. Must derive from <see cref="Component"/>.</typeparam>
    /// <param name="o">The <see cref="GameObject"/> from which to retrieve the components. Can be null.</param>
    /// <returns>An array of components of type <typeparamref name="T"/> found in the <see cref="GameObject"/> and its
    /// children.  Returns an empty array if the <paramref name="o"/> is null.</returns>
    public static T[] SafeGetComponentsInChildren<T>(this GameObject? o) where T : Component
    {
        if (o.IsNull())
            return [];
        return o.GetComponentsInChildren<T>();
    }

    /// <summary>
    /// Retrieves the first component of type <typeparamref name="T"/> in the specified <see cref="GameObject"/> or
    /// its children.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="o">The <see cref="GameObject"/> to search. Can be <see langword="null"/>.</param>
    /// <param name="includeInactive">A value indicating whether to include inactive GameObjects in the search.  <see langword="true"/> to include
    /// inactive GameObjects; otherwise, <see langword="false"/>.</param>
    /// <returns>The first component of type <typeparamref name="T"/> found in the <paramref name="o"/> or its children,  or
    /// <see langword="null"/> if no such component is found or if <paramref name="o"/> is <see langword="null"/>.</returns>
    public static T? SafeGetComponentInChildren<T>(this GameObject? o, bool includeInactive = false) where T : Component
    {
        if (o.IsNull())
            return null;
        return o.GetComponentInChildren<T>(includeInactive);
    }

    /// <summary>
    /// Retrieves the first component of type <typeparamref name="T"/> from the specified component or its children.
    /// Returns <see langword="null"/> if the transform is <see langword="null"/> or if no such component is found.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="c">The Component from which to search for the sibling or contained component.</param>
    /// <param name="includeInactive">Whether to include inactive child GameObjects in the search.</param>
    /// <returns>The first component of type <typeparamref name="T"/> found, or <see langword="null"/> if none is found.</returns>
    public static T? SafeGetComponentInChildren<T>(this Component? c, bool includeInactive = false) where T : Component
    {
        if (c.IsNull())
            return null;
        return c.GetComponentInChildren<T>(includeInactive);
    }

    /// <summary>
    /// Retrieves the first component of type <typeparamref name="T"/> in the parent hierarchy of the specified component.
    /// Returns null if the component is null or if no such component is found in the parent hierarchy.
    /// </summary>
    /// <typeparam name="T">The type of component to retrieve.</typeparam>
    /// <param name="c">The Component from which to search for the sibling or parent component.</param>
    /// <returns>The first component of type <typeparamref name="T"/> found, or <see langword="null"/> if none is found.</returns>
    public static T? SafeGetComponentInParent<T>(this Component? c) where T : Component
    {
        if (c.IsNull())
            return null;
        return c.GetComponentInParent<T>();
    }

    /// <summary>
    /// Selectively gets a component of type <typeparamref name="T"/> from a sibling component.
    /// If the component is null, returns null.
    /// </summary>
    /// <typeparam name="T">Requested component type</typeparam>
    /// <param name="c">Component to get the sibling component of</param>
    /// <returns>Requested component or null</returns>
    public static T? SafeGetComponent<T>(this Component? c) where T : Component
    {
        if (c.IsNull())
            return null;
        return c.GetComponent<T>();
    }

    /// <summary>
    /// Retrieves a component of the specified type from the given <see cref="GameObject"/>
    /// only if the game object is not null.
    /// Otherwise, returns null.
    /// </summary>
    /// <typeparam name="T">The type of the component to retrieve. Must derive from <see cref="Component"/>.</typeparam>
    /// <param name="go">The <see cref="GameObject"/> from which to retrieve the component. Can be <see langword="null"/>.</param>
    /// <returns>The component of type <typeparamref name="T"/> if found; otherwise, <see langword="null"/>.  Returns <see
    /// langword="null"/> if <paramref name="go"/> is <see langword="null"/>.</returns>
    public static T? SafeGetComponent<T>(this GameObject? go) where T : Component
    {
        if (go.IsNull())
            return null;
        return go.GetComponent<T>();
    }

    /// <summary>
    /// Returns the parent <see cref="Transform"/> of the given <paramref name="t"/>,
    /// or <see langword="null"/> if <paramref name="t"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="t">The <see cref="Transform"/> whose parent is to be retrieved. Can be <see langword="null"/>.</param>
    /// <returns>The parent <see cref="Transform"/>, or <see langword="null"/> if <paramref name="t"/> is <see langword="null"/>.</returns>
    public static Transform? SafeGetParent(this Transform? t)
    {
        if (t.IsNull())
            return null;
        return t.parent;
    }


    /// <summary>
    /// Attempts to retrieve the player's current vehicle as a specific type.
    /// </summary>
    /// <typeparam name="T">The type of vehicle to retrieve. Must derive from <see cref="Vehicle"/>.</typeparam>
    /// <param name="player">The player whose vehicle is being queried. Can be <see langword="null"/>.</param>
    /// <returns>
    /// The player's current vehicle cast to type <typeparamref name="T"/>, or <see langword="null"/> if the player is <see langword="null"/>,
    /// the player is not in a vehicle, or the vehicle is not of type <typeparamref name="T"/>.
    /// </returns>
    public static T? SafeGetVehicle<T>(this Player? player) where T : Vehicle
    {
        if (player.IsNull())
            return null;
        return player.GetVehicle() as T;
    }


    /// <summary>
    /// Sets the active state of the specified <see cref="GameObject"/> if it is not null.
    /// Does nothing if the <paramref name="gameObject"/> is null.
    /// </summary>
    /// <param name="gameObject">The <see cref="GameObject"/> to set active or inactive. Can be null.</param>
    /// <param name="value">The active state to set.</param>
    public static void SafeSetActive(this GameObject? gameObject, bool value)
    {
        if (gameObject.IsNull())
            return;
        gameObject.SetActive(value);
    }

    /// <summary>
    /// Executes the specified action if the object is not null.
    /// </summary>
    /// <remarks>This method provides a safe way to perform an action on a nullable object derived
    /// from <see cref="UnityEngine.Object"/>. If <paramref name="item"/> is null, the method does
    /// nothing.</remarks>
    /// <typeparam name="T">The type of the object, which must derive from <see cref="UnityEngine.Object"/>.</typeparam>
    /// <param name="item">The object to check for null before executing the action.</param>
    /// <param name="action">The action to execute if <paramref name="item"/> is not null.</param>
    /// <returns><see langword="true"/> if the action was executed; <see langword="false"/> if <paramref name="item"/> was null.</returns>
    public static bool SafeDo<T>(this T? item, Action<T> action) where T : Object
    {
        if (item.IsNull())
            return false;
        action(item);
        return true;
    }

    /// <summary>
    /// Safely retrieves a value from an object using a getter function, returning a fallback value if the object is null.
    /// </summary>
    /// <typeparam name="TObject">Object type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    /// <param name="item">Item to get a value of</param>
    /// <param name="getter">Getter function</param>
    /// <param name="fallback">Fallback value if <paramref name="item"/> is null</param>
    /// <returns>Result of <paramref name="getter"/> if <paramref name="item"/> is not null, <paramref name="fallback"/> otherwise</returns>
    public static TValue SafeGet<TObject, TValue>(this TObject? item, Func<TObject, TValue> getter, TValue fallback)
        where TObject : Object
    {
        if (item.IsNull())
            return fallback;
        return getter(item);
    }

    /// <summary>
    /// Retrieves the <see cref="PrefabIdentifier"/> component attached to the specified component.
    /// </summary>
    /// <param name="c">The component from which to retrieve the <see cref="PrefabIdentifier"/>. Can be <see langword="null"/>.</param>
    /// <returns>The <see cref="PrefabIdentifier"/> component if found; otherwise, <see langword="null"/>.</returns>
    public static PrefabIdentifier? PrefabId(this Component? c)
    {
        if (c.IsNull())
            return null;
        return c.GetComponent<PrefabIdentifier>();
    }


    /// <summary>
    /// Retrieves the <see cref="PrefabIdentifier"/> component attached to the specified game object.
    /// </summary>
    /// <returns>The <see cref="PrefabIdentifier"/> component if found; otherwise, <see langword="null"/>.</returns>
    public static PrefabIdentifier? PrefabId(this GameObject? o)
    {
        if (o.IsNull())
            return null;
        return o.GetComponent<PrefabIdentifier>();
    }


    /// <summary>
    /// Extension method to write reflected data associated with a prefab identifier to a JSON file of the current save game slot.
    /// </summary>
    public static bool WriteReflected<T>(this PrefabIdentifier? prefabID, string prefix, T data, RootModController rmc) =>
        SaveFiles.Current.WritePrefabReflected(prefabID, prefix, data, rmc);

    /// <summary>
    /// Extension method to write data associated with a prefab identifier to a JSON file of the current save game slot.
    /// </summary>
    public static bool WriteData(this PrefabIdentifier? prefabID, string prefix, Data data, RootModController rmc) =>
        SaveFiles.Current.WritePrefabData(prefabID, prefix, data, rmc);

    /// <summary>
    /// Extension method to read data via reflection from a JSON file in the current save game slot.
    /// </summary>
    public static bool ReadReflected<T>(this PrefabIdentifier? prefabID, string prefix, [NotNullWhen(true)] out T? data,
        RootModController rmc)
        where T : class =>
        SaveFiles.Current.ReadPrefabReflected(prefabID, prefix, out data, rmc);

    /// <summary>
    /// Extension method to read data associated with a prefab identifier from a JSON file in the current save game slot.
    /// </summary>
    public static bool ReadData(this PrefabIdentifier? prefabID, string prefix, Data data, RootModController rmc) =>
        SaveFiles.Current.ReadPrefabData(prefabID, prefix, data, rmc);

    /// <summary>
    /// Resolves the vehicle name of a vehicle.
    /// </summary>
    /// <param name="vehicle">Vehicle to get the name of</param>
    /// <returns>Vehicle name or "&lt;null&gt;"</returns>
    public static string GetVehicleName(this Vehicle? vehicle)
    {
        if (vehicle.IsNull())
            return "<null>";
        return vehicle.subName.IsNotNull() ? vehicle.subName.GetName() : vehicle.vehicleName;
    }

    /// <summary>
    /// Sets a material on a specific slot of a renderer.
    /// </summary>
    /// <param name="renderer">The renderer to set the material of</param>
    /// <param name="index">The index of the material on the renderer</param>
    /// <param name="material">The material to set</param>
    public static void ReplaceMaterial(this Renderer? renderer, int index, Material material)
    {
        if (renderer.IsNull())
        {
            Logger.Error("Renderer is null, cannot set material.");
            return;
        }

        if (index < 0 || index >= renderer.materials.Length)
        {
            Logger.Error($"Invalid material slot {index} for renderer {renderer.NiceName()}");
            return;
        }

        Material[] materials = renderer.materials;
        materials[index] = material;
        renderer.materials = materials;
    }

    /// <summary>
    /// Destroys all child transforms of <paramref name="transform"/>
    /// </summary>
    /// <remarks>
    /// The children will actually be destroyed at the end of the frame, but they will immediately be de-parented
    /// </remarks>
    /// <param name="transform">Parent transform to destroy all children of</param>
    public static void DestroyChildren(this Transform? transform)
    {
        foreach (var tran in transform.SafeGetChildren().ToList())
        {
            tran.parent = null; // detach from parent
            Object.Destroy(tran.gameObject);
        }
    }

    /// <summary>
    /// Safely checks if a Unity object is null
    /// </summary>
    /// <param name="o">Object to check</param>
    /// <returns>True if null, false if not null</returns>
    public static bool IsNull([NotNullWhen(false)] this Object? o)
        => !o;

    /// <summary>
    /// Safely checks if a Unity object is not null
    /// </summary>
    /// <param name="o">Object to check</param>
    /// <returns>True if not null, false if null</returns>
    public static bool IsNotNull([NotNullWhen(true)] this Object? o)
        => (bool)o;
}