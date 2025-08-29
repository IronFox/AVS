using AVS.Interfaces;
using AVS.Log;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace AVS.SaveLoad;

/// <summary>
/// Captures and restores data produced by an entity.
/// </summary>
public class Data : INullTestableType
{
    /// <summary>
    /// Creates a new file with the given name and blocks.
    /// </summary>
    /// <param name="name">File name for debugging and logging purposes.
    /// Not persisted to JSON.</param>
    /// <param name="blocks">File blocks, each containing a collection of properties.</param>
    public Data(string name, params DataBlock[] blocks)
    {
        Name = name;
        Blocks = blocks;
    }

    /// <summary>
    /// File name for debugging and logging purposes.
    /// Not persisted to JSON.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// File blocks, each containing a collection of properties.
    /// </summary>
    public DataBlock[] Blocks { get; }

    internal JObject ToJson()
    {
        var json = new JObject();
        foreach (var block in Blocks)
            json[block.Name] = block.ToJson();
        return json;
    }

    internal void FromJson(JObject json, SmartLog writer)
    {
        foreach (var block in Blocks)
            if (json.TryGetValue(block.Name, out var value) && value is JObject jObject)
                block.FromJson(jObject, writer);
            else
                writer.Warn($"Block '{block.Name}' not found in JSON or is not an object.");
    }
}

/// <summary>
/// Data associated with a specific save context (e.g. a vehicle type specific data).
/// Each instance should be handled by one class only.
/// </summary>
public class DataBlock : IEnumerable<IPersistable>
{
    /// <summary>
    /// Gets an enumerator that iterates through the collection of properties.
    /// </summary>
    /// <returns>An enumerator for the collection of properties.</returns>
    public IEnumerator<IPersistable> GetEnumerator() => ((IEnumerable<IPersistable>)Properties).GetEnumerator();

    /// <summary>
    /// Gets an enumerator that iterates through a collection.
    /// </summary>
    /// <returns>An enumerator for the collection.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Creates a new file block with the given name and properties.
    /// </summary>
    /// <param name="name">File block name</param>
    /// <param name="properties">Contained properties</param>
    public DataBlock(string name, params IPersistable[] properties)
    {
        Name = name;
        this.properties.AddRange(properties);
    }

    /// <summary>
    /// Creates a new file block with the given name and properties.
    /// </summary>
    /// <param name="name">File block name</param>
    /// <param name="properties">Contained properties</param>
    public DataBlock(string name, IEnumerable<IPersistable> properties)
    {
        Name = name;
        this.properties.AddRange(properties);
    }

    /// <summary>
    /// Appends a new property to the block.
    /// </summary>
    /// <param name="property">New property</param>
    public void Add(IPersistable property)
    {
        properties.Add(property);
    }

    /// <summary>
    /// File block name, used to identify the block in the JSON file.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the collection of properties associated with the current object.
    /// </summary>
    public IReadOnlyList<IPersistable> Properties => properties;

    private List<IPersistable> properties = new();

    internal JObject ToJson()
    {
        var json = new JObject();
        foreach (var property in Properties)
            json[property.Name] = property.ExportValue();
        return json;
    }

    internal void FromJson(JObject json, SmartLog writer)
    {
        foreach (var property in Properties)
            if (json.TryGetValue(property.Name, out var value))
            {
                if (value is JToken jValue)
                    property.RestoreValue(jValue, writer);
                else
                    writer.Warn($"Property '{property.Name}' in JSON for block '{Name}' is not a JToken.");
            }
            else
            {
                writer.Warn($"Property '{property.Name}' not found in JSON for block '{Name}'.");
            }
    }
}

/// <summary>
/// Data entry that can be persisted to a JSON file.
/// </summary>
public interface IPersistable
{
    /// <summary>
    /// Name of the property as written to JSON.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Exports the value of the property as a value that can be written to JSON.
    /// </summary>
    /// <returns>Value to be saved.</returns>
    JToken ExportValue();

    /// <summary>
    /// Restores the value of the property.
    /// </summary>
    /// <param name="value">Value to restore.</param>
    /// <param name="writer">Log writer to use for logging.</param>
    void RestoreValue(JToken value, SmartLog writer);
}

/// <summary>
/// Helper class to create persistent properties.
/// </summary>
public static class Persistable
{
    /// <summary>
    /// Creates a persistent property with the given name, getter and setter.
    /// </summary>
    /// <typeparam name="T">Type preserved in JSON. Should be a simple type (bool, int, string, ...)</typeparam>
    /// <param name="name">Name of the property.</param>
    /// <param name="get">Function to get the value.</param>
    /// <param name="set">Action to set the value.</param>
    /// <returns>New persistent property.</returns>
    public static PersistentProperty<T> Property<T>(string name, Func<T> get, Action<T> set) => new(name, get, set);
}

/// <summary>
/// Property written to and restored from a JSON file.
/// </summary>
/// <typeparam name="T">Type preserved in JSON. Should be a simple type (bool, int, string, ...)
/// </typeparam>
public class PersistentProperty<T> : IPersistable
{
    internal PersistentProperty(string name, Func<T> exportValue, Action<T> applyImportedValue)
    {
        Name = name;
        ExportValue = exportValue;
        ApplyImportedValue = applyImportedValue;
    }

    /// <summary>
    /// The name of the property as written to JSON.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The function to export the value of the property.
    /// </summary>
    public Func<T> ExportValue { get; }

    /// <summary>
    /// Gets the action that reapply the value to its owner.
    /// </summary>
    public Action<T> ApplyImportedValue { get; }


    string IPersistable.Name => Name;

    JToken IPersistable.ExportValue()
    {
        var value = ExportValue();
        return JSONSerialization.ToJson(value);
    }

    void IPersistable.RestoreValue(JToken value, SmartLog writer)
    {
        try
        {
            var v = JSONSerialization.FromJson<T>(value);
            if (typeof(T).IsClass && Equals(v, default(T)))
            {
                writer.Warn($"Property '{Name}' was not restored because the value [{value}] is null or default.");
                return;
            }

            ApplyImportedValue(v!);
        }
        catch (Exception ex)
        {
            writer.Error($"Failed to restore property '{Name}' with value [{value}]: {ex.Message}");
        }
    }
}