using AVS.Interfaces;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;

namespace AVS.Util;

/// <summary>
/// Various null testing extensions for non-Unity types
/// </summary>
public static class IsNullExtensions
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static bool IsNull<K, V>([NotNullWhen(false)] this Dictionary<K, V>? en)
        => en is null;
    public static bool IsNullOrEmpty<K, V>([NotNullWhen(false)] this Dictionary<K, V>? en)
        => en is null || en.Count == 0;

    public static bool IsNull<K, V>([NotNullWhen(false)] this IReadOnlyDictionary<K, V>? en)
        => en is null;

    public static bool IsNullOrEmpty<K, V>([NotNullWhen(false)] this IReadOnlyDictionary<K, V>? en)
        => en is null || en.Count == 0;

    public static bool IsNull([NotNullWhen(false)] this IDictionary? en)
        => en is null;

    public static bool IsNullOrEmpty([NotNullWhen(false)] this IDictionary? en)
        => en is null || en.Count == 0;

    public static bool IsNull<T>([NotNullWhen(false)] this IReadOnlyList<T>? en)
        => en is null;

    public static bool IsNullOrEmpty<T>([NotNullWhen(false)] this IReadOnlyList<T>? en)
        => en is null || en.Count == 0;

    public static bool IsNull([NotNullWhen(false)] this Exception? en)
        => en is null;

    public static bool IsNull([NotNullWhen(false)] this InventoryItem? item)
        => item is null;

    public static bool IsNull([NotNullWhen(false)] this Coroutine? item)
        => item is null;

    public static bool IsNull([NotNullWhen(false)] this PropertyInfo? item)
        => item is null;

    public static bool IsNull([NotNullWhen(false)] this FieldInfo? item)
        => item is null;

    public static bool IsNull([NotNullWhen(false)] this MethodInfo? item)
        => item is null;

    public static bool IsNull([NotNullWhen(false)] this INullTestableType? item)
        => item is null;

    public static bool IsNull([NotNullWhen(false)] this CodeInstruction? item)
        => item is null;

    public static bool IsNull([NotNullWhen(false)] this Type? type)
        => type is null;

    public static bool IsNull([NotNullWhen(false)] this IItemsContainer? acc)
        => acc is null;

    public static bool IsNull([NotNullWhen(false)] this ItemsContainer? acc)
        => acc is null;

    public static bool IsNull<T>([NoEnumeration][NotNullWhen(false)] this IEnumerable<T>? en)
        => en is null;

    public static bool IsNull<T>([NotNullWhen(false)] this T? item) where T : struct
        => !item.HasValue;

    public static bool IsNull([NotNullWhen(false)] this string? s)
        => s is null;
    public static bool IsNullOrEmpty([NotNullWhen(false)] this string? s)
        => string.IsNullOrEmpty(s);

    public static bool IsNull([NotNullWhen(false)] this Action? a)
        => a is null;

    public static bool IsNull<T0>([NotNullWhen(false)] this Action<T0>? a)
        => a is null;

    public static bool IsNull<T0, T1>([NotNullWhen(false)] this Action<T0, T1>? a)
        => a is null;
    public static bool IsNull<T0, T1, T2>([NotNullWhen(false)] this Action<T0, T1, T2>? a)
        => a is null;
    public static bool IsNull<T0, T1, T2, T3>([NotNullWhen(false)] this Action<T0, T1, T2, T3>? a)
        => a is null;
    public static bool IsNull<T0, T1, T2, T3, T4>([NotNullWhen(false)] this Action<T0, T1, T2, T3, T4>? a)
        => a is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5>([NotNullWhen(false)] this Action<T0, T1, T2, T3, T4, T5>? a)
        => a is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6>([NotNullWhen(false)] this Action<T0, T1, T2, T3, T4, T5, T6>? a)
        => a is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7>([NotNullWhen(false)] this Action<T0, T1, T2, T3, T4, T5, T6, T7>? a)
        => a is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T8>([NotNullWhen(false)] this Action<T0, T1, T2, T3, T4, T5, T6, T7, T8>? a)
        => a is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>([NotNullWhen(false)] this Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>? a)
        => a is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>([NotNullWhen(false)] this Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? a)
        => a is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>([NotNullWhen(false)] this Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? a)
        => a is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>([NotNullWhen(false)] this Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>? a)
        => a is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>([NotNullWhen(false)] this Action<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>? a)
        => a is null;

    public static bool IsNull<T>([NotNullWhen(false)] this Func<T>? f)
        => f is null;
    public static bool IsNull<T0, T>([NotNullWhen(false)] this Func<T0, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T>([NotNullWhen(false)] this Func<T0, T1, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T2, T>([NotNullWhen(false)] this Func<T0, T1, T2, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T2, T3, T>([NotNullWhen(false)] this Func<T0, T1, T2, T3, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T>([NotNullWhen(false)] this Func<T0, T1, T2, T3, T4, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T>([NotNullWhen(false)] this Func<T0, T1, T2, T3, T4, T5, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T>([NotNullWhen(false)] this Func<T0, T1, T2, T3, T4, T5, T6, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T>([NotNullWhen(false)] this Func<T0, T1, T2, T3, T4, T5, T6, T7, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T8, T>([NotNullWhen(false)] this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T>([NotNullWhen(false)] this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T>([NotNullWhen(false)] this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T>([NotNullWhen(false)] this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T>([NotNullWhen(false)] this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T>? f)
        => f is null;
    public static bool IsNull<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T>([NotNullWhen(false)] this Func<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T>? f)
        => f is null;
}