using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AVS.Interfaces;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace AVS.Util;

/// <summary>
/// Various null testing extensions for non-Unity types
/// </summary>
public static class IsNotNullExtensions
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public static bool IsNotNull<K, V>([NotNullWhen(true)] this Dictionary<K, V>? en)
        => en is not null;

    public static bool IsNotNull<K, V>([NotNullWhen(true)] this IReadOnlyDictionary<K, V>? en)
        => en is not null;

    public static bool IsNotNull([NotNullWhen(true)] this IDictionary? en)
        => en is not null;

    public static bool IsNotNull([NotNullWhen(true)] this Exception? en)
        => en is not null;

    public static bool IsNotNull([NotNullWhen(true)] this InventoryItem? item)
        => item is not null;

    public static bool IsNotNull([NotNullWhen(true)] this Coroutine? item)
        => item is not null;

    public static bool IsNotNull([NotNullWhen(true)] this PropertyInfo? item)
        => item is not null;

    public static bool IsNotNull([NotNullWhen(true)] this FieldInfo? item)
        => item is not null;

    public static bool IsNotNull([NotNullWhen(true)] this MethodInfo? item)
        => item is not null;

    public static bool IsNotNull([NotNullWhen(true)] this INullTestableType? item)
        => item is not null;

    public static bool IsNotNull([NotNullWhen(true)] this CodeInstruction? item)
        => item is not null;

    public static bool IsNotNull<T>([NoEnumeration] [NotNullWhen(true)] this IEnumerable<T>? en)
        => en is not null;

    public static bool IsNotNull<T>([NotNullWhen(true)] this T? item) where T : struct
        => item.HasValue;

    public static bool IsNotNull([NotNullWhen(true)] this Action? acc)
        => acc is not null;

    public static bool IsNotNull<T0>([NotNullWhen(true)] this Action<T0>? acc)
        => acc is not null;

    public static bool IsNotNull<T0, T1>([NotNullWhen(true)] this Action<T0, T1>? acc)
        => acc is not null;

    public static bool IsNotNull<T0, T1, T2>([NotNullWhen(true)] this Action<T0, T1, T2>? acc)
        => acc is not null;

    public static bool IsNotNull<T0, T1, T2, T3>([NotNullWhen(true)] this Action<T0, T1, T2, T3>? acc)
        => acc is not null;

    public static bool IsNotNull<T0, T1, T2, T3, T4>([NotNullWhen(true)] this Action<T0, T1, T2, T3, T4>? acc)
        => acc is not null;

    public static bool IsNotNull<T0, T1, T2, T3, T4, T5>([NotNullWhen(true)] this Action<T0, T1, T2, T3, T4, T5>? acc)
        => acc is not null;

    public static bool IsNotNull<T0, T1, T2, T3, T4, T5, T6>(
        [NotNullWhen(true)] this Action<T0, T1, T2, T3, T4, T5, T6>? acc)
        => acc is not null;

    public static bool IsNotNull<T0, T1, T2, T3, T4, T5, T6, T7>(
        [NotNullWhen(true)] this Action<T0, T1, T2, T3, T4, T5, T6, T7>? acc)
        => acc is not null;

    public static bool IsNotNull<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
        [NotNullWhen(true)] this Action<T0, T1, T2, T3, T4, T5, T6, T7, T8>? acc)
        => acc is not null;

    public static bool IsNotNull<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        [NotNullWhen(true)] this Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? acc)
        => acc is not null;

    public static bool IsNotNull([NotNullWhen(true)] this Type? type)
        => type is not null;

    public static bool IsNotNull([NotNullWhen(true)] this IItemsContainer? item)
        => item is not null;

    public static bool IsNotNull([NotNullWhen(true)] this ItemsContainer? item)
        => item is not null;

    public static bool IsNotNull([NotNullWhen(true)] this NotificationCenter.Notification? item)
        => item is not null;
}