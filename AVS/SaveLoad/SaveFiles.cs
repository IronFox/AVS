using AVS.Log;
using AVS.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;

namespace AVS.SaveLoad;

/// <summary>
/// Files of one save slot.
/// </summary>
internal class SaveFiles
{
    private SaveFiles(string slot)
    {
        Slot = slot;
    }


    /// <summary>
    /// Accesses the current save slot files.
    /// </summary>
    public static SaveFiles Current => OfSlot(SaveLoadManager.main.GetCurrentSlot());

    /// <summary>
    /// Accesses the save files of a specific slot.
    /// </summary>
    /// <param name="slot">Slot to access</param>
    public static SaveFiles OfSlot(string slot) => new(slot);


    public static string SaveFolderName { get; } = "AVS";

    /// <summary>
    /// Save game slot identifier.
    /// </summary>
    public string Slot { get; }


    /// <summary>
    /// Writes data associated with a prefab identifier to a JSON file.
    /// </summary>
    /// <typeparam name="T">Type of the data to write</typeparam>
    /// <param name="prefabID">Prefab identifier to write to</param>
    /// <param name="prefix">File prefix</param>
    /// <param name="data">Data to write</param>
    /// <param name="rmc">Owning root mod controller</param>
    public bool WritePrefabReflected<T>(PrefabIdentifier? prefabID, string prefix, T data,
        RootModController rmc)
    {
        using var log = SmartLog.ForAVS(rmc, tags: Tags);
        if (prefabID.IsNull())
        {
            log.Error($"PrefabIdentifier is null, cannot write: {prefix}");
            return false;
        }

        var fname = $"{prefix}-{prefabID.Id}";
        string json;
        try
        {
            json = JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        catch (Exception e)
        {
            log.Error($"Failed to serialize json data for file {fname}", e);
            return false;
        }

        return WriteJson($"{prefix}-{prefabID.Id}", json, rmc);
    }


    /// <summary>
    /// Writes data associated with a prefab identifier to a JSON file.
    /// </summary>
    /// <param name="prefabID">Prefab identifier to write to</param>
    /// <param name="prefix">File prefix</param>
    /// <param name="data">Data to write</param>
    /// <param name="rmc">Owning root mod controller</param>
    /// <returns>True if the data was successfully written, false otherwise.</returns>
    public bool WritePrefabData(PrefabIdentifier? prefabID, string prefix, Data data,
        RootModController rmc)
    {
        using var writer = SmartLog.ForAVS(rmc, tags: Tags);
        if (prefabID.IsNull())
        {
            writer.Error($"PrefabIdentifier is null, cannot write: {prefix}");
            return false;
        }

        var fname = $"{prefix}-{prefabID.Id}";
        string json;
        try
        {
            json = JsonConvert.SerializeObject(data.ToJson(), Formatting.Indented);
        }
        catch (Exception e)
        {
            writer.Error($"Failed to serialize json data for file {fname}", e);
            return false;
        }


        return WriteJson(fname, json, rmc);
    }

    private static IReadOnlyList<string> Tags { get; } = ["IO"];

    /// <summary>
    /// Serializes the specified data to JSON and writes it to a file.
    /// </summary>
    /// <remarks>If serialization fails, an error is logged using the provided <paramref
    /// name="rmc"/>, and the method returns <see langword="false"/>. If the file writing operation fails, the
    /// method also returns <see langword="false"/>.</remarks>
    /// <typeparam name="T">The type of the data to be serialized.</typeparam>
    /// <param name="innerName">Inner file name without folder or trailing extension</param>
    /// <param name="data">The object to serialize into JSON. Cannot be null.</param>
    /// <param name="rmc">Owning root mod controller</param>
    /// <returns><see langword="true"/> if the JSON data was successfully written to the file; otherwise, <see
    /// langword="false"/>.</returns>
    internal bool WriteReflected<T>(string innerName, T data,
        RootModController rmc)
    {
        using var writer = SmartLog.ForAVS(rmc, tags: Tags);
        string json;
        try
        {
            json = JsonConvert.SerializeObject(data, Formatting.Indented);
        }
        catch (Exception e)
        {
            writer.Error($"Failed to serialize json data for file {innerName}", e);
            return false;
        }

        return WriteJson(innerName, json, rmc);
    }

    /// <summary>
    /// Reads JSON data from files and deserializes it into an object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type to deserialize from JSON</typeparam>
    /// <param name="innerName">Inner file name without folder or trailing extension</param>
    /// <param name="data">Deserialized data. Null if loading has failed</param>
    /// <param name="rmc">Owning root mod controller</param>
    /// <returns>True if loading has succeeded and produced non-null data</returns>
    internal bool ReadReflected<T>(string innerName, out T? data, RootModController rmc) where T : class
    {
        data = ReadJson<T>(innerName, rmc);
        return data != null;
    }


    /// <summary>
    /// Deserializes JSON data for a specified prefab identifier.
    /// </summary>
    /// <typeparam name="T">Type to deserialize from JSON</typeparam>
    /// <param name="prefabID">The identifier of the prefab to read. Cannot be <see langword="null"/>.</param>
    /// <param name="prefix">A string prefix used to construct the JSON file name.</param>
    /// <param name="outData">Deserialized data</param>
    /// <param name="rmc">Owning root mod controller</param>
    /// <returns>True on success</returns>
    public bool ReadPrefabReflected<T>(
        PrefabIdentifier? prefabID,
        string prefix,
        [NotNullWhen(true)] out T? outData,
        RootModController rmc) where T : class
    {
        using var log = SmartLog.ForAVS(rmc, tags: Tags, parameters: Params.Of(prefabID, prefix));
        if (prefabID.IsNull())
        {
            log.Error($"PrefabIdentifier is null, cannot read: {prefix}");
            outData = null;
            return false;
        }

        outData = ReadJson<T>($"{prefix}-{prefabID.Id}", rmc);
        return outData != null;
    }

    /// <summary>
    /// Reads and processes JSON data for a specified prefab identifier.
    /// </summary>
    /// <remarks>This method attempts to read JSON data associated with the given <paramref
    /// name="prefabID"/> and populate the provided <paramref name="data"/> object. If <paramref name="prefabID"/>
    /// is <see langword="null"/>, or if the JSON data cannot be read, an error is logged.</remarks>
    /// <param name="prefabID">The identifier of the prefab to read. Cannot be <see langword="null"/>.</param>
    /// <param name="prefix">A string prefix used to construct the JSON file name.</param>
    /// <param name="data">The data object to populate with the JSON content.</param>
    /// <param name="rmc">Owning root mod controller.</param>
    public bool ReadPrefabData(PrefabIdentifier? prefabID, string prefix, Data data, RootModController rmc)
    {
        using var log = SmartLog.ForAVS(rmc, tags: Tags, parameters: Params.Of(prefabID, prefix));

        if (prefabID.IsNull())
        {
            log.Error($"PrefabIdentifier is null, cannot read: {prefix}");
            return false;
        }

        var json = ReadJson<JObject>($"{prefix}-{prefabID.Id}", rmc);
        if (json is not null)
        {
            data.FromJson(json, log);
            return true;
        }
        else
        {
            log.Error($"Failed to read JSON for {prefix}-{prefabID.Id}");
            return false;
        }
    }

    /// <summary>
    /// Writes JSON data to files based on the provided filename.
    /// </summary>
    /// <param name="innerName">Inner file name without folder or trailing extension</param>
    /// <param name="json">Serialized JSON</param>
    /// <param name="rmc">Owning root mod controller</param>
    /// <returns>True if the file was successfully written</returns>
    private bool WriteJson(string innerName, string json,
        RootModController rmc)
    {
        using var writer = SmartLog.ForAVS(rmc, tags: Tags);
        var files = new List<FilePath>();
        try
        {
            files.AddRange(GetFiles(innerName));
        }
        catch (Exception e)
        {
            writer.Error($"Failed to convert filename '{innerName}' to files", e);
            return false;
        }

        string base64Hash;
        try
        {
            using var sha256 = SHA256.Create();
            var hashedJson = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
            base64Hash = Convert.ToBase64String(hashedJson);
        }
        catch (Exception e)
        {
            writer.Error($"Failed to compute hash value of string of length {json.Length}", e);
            return false;
        }


        var fileContent = $"{{\"hash\":\"{base64Hash}\",\"data\":{json}}}";
        if (fileContent.Length > 100_000_000) // ~100 MB
        {
            writer.Error($"File content is too large ({fileContent.Length} bytes), skipping.");
            return false;
        }

        var written = 0;
        foreach (var file in files)
            try
            {
                if (!file.IsValid)
                {
                    writer.Error($"Out file path too long ({file.FullName.Length}): {file.FullName}. Skipping");
                    continue;
                }

                file.WriteAllText(fileContent);
                writer.Debug($"Wrote file {file.FullName} with length {fileContent.Length}");
                written++;
            }
            catch (Exception e)
            {
                writer.Error($"Failed to process file {file.FullName}", e);
                continue;
            }

        return written > 0;
    }


    private T? ReadJson<T>(string innerName, RootModController rmc) where T : class
    {
        using var log = SmartLog.ForAVS(rmc, tags: Tags, parameters: Params.Of(innerName));
        var files = new List<FilePath>();
        try
        {
            files.AddRange(GetFiles(innerName));
        }
        catch (Exception e)
        {
            log.Error($"Failed to convert filename '{innerName}' to files in slot {Slot}", e);
            return null;
        }

        foreach (var file in files)
        {
            if (!file.IsFile)
            {
                log.Debug($"File does not exist: {file.FullName}");
                continue;
            }

            if (file.FileSize > 100_000_000) // ~100 MB
            {
                log.Error($"File {file.FullName} is too large ({file.FileSize} bytes), skipping.");
                continue;
            }

            try
            {
                var json = file.ReadAllText();
                // Manually decode: {"hash":"...","data":...}
                var hashIdx = json.IndexOf("\"hash\"");
                var dataIdx = json.IndexOf("\"data\"");
                if (hashIdx == -1 || dataIdx == -1)
                {
                    log.Error($"File {file.FullName} does not contain 'hash' or 'data' fields.");
                    continue;
                }

                var hashStart = json.IndexOf(':', hashIdx) + 1;
                var hashQuote1 = json.IndexOf('"', hashStart);
                var hashQuote2 = json.IndexOf('"', hashQuote1 + 1);
                var hashToken = json.Substring(hashQuote1 + 1, hashQuote2 - hashQuote1 - 1);

                var dataStart = json.IndexOf(':', dataIdx) + 1;
                // Find the start of the data (skip whitespace)
                while (dataStart < json.Length && char.IsWhiteSpace(json[dataStart])) dataStart++;
                // Data can be any JSON value (object, array, string, etc.)
                // We assume it's an object or array, so we find the matching bracket
                var dataEnd = json.LastIndexOf('}');
                if (dataEnd <= dataStart)
                {
                    log.Error($"Malformed JSON in file {file.FullName} (data field).");
                    continue;
                }

                // The data field may be followed by a closing } (end of root object)
                // So we want to extract from dataStart to dataEnd (exclusive of last })
                var dataJson = json.Substring(dataStart, dataEnd - dataStart).TrimEnd();
                // Remove trailing comma if present
                if (dataJson.EndsWith(","))
                    dataJson = dataJson.Substring(0, dataJson.Length - 1);

                using var sha256 = SHA256.Create();
                var hashedJson = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dataJson));
                var base64Hash = Convert.ToBase64String(hashedJson);
                if (base64Hash == hashToken)
                {
                    // Parse dataJson as JToken
                    var rs = JsonConvert.DeserializeObject<T>(dataJson) ??
                             throw new JsonSerializationException($"Failed to deserialize data for {innerName}");
                    log.Debug($"Successfully read file {file.FullName} with length {dataJson.Length}");
                    return rs;
                }
                else
                {
                    log.Error(
                        $"Hash mismatch for file {file.FullName}. Expected: {hashToken}, Actual: {base64Hash}");
                }
            }
            catch (Exception e)
            {
                log.Error($"Failed to read or parse file {file.FullName}", e);
            }
        }

        log.Error($"No valid JSON data found in files for {innerName} in slot {Slot}");
        return null;
    }


    private IEnumerable<FilePath> GetFiles(string innerName)
    {
        var configFolderPath = Path.Combine(PlatformServicesNull.DefaultSavePath, Slot, SaveFolderName);
        if (!Directory.Exists(configFolderPath))
            Directory.CreateDirectory(configFolderPath);
        yield return new FilePath(configFolderPath, $"{innerName}.json");
        yield return new FilePath(configFolderPath, $"{innerName}-fb.json");
    }
}