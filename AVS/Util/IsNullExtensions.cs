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
}