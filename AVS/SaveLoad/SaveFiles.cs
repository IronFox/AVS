using AVS.Log;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;

namespace AVS.SaveLoad
{
    /// <summary>
    /// Files of one save slot.
    /// </summary>
    public class SaveFiles
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
        public static SaveFiles OfSlot(string slot) => new SaveFiles(slot);


        public static string SaveFolderName { get; } = "AVS";
        public string Slot { get; }







        /// <summary>
        /// Writes data associated with a prefab identifier to a JSON file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prefabID"></param>
        /// <param name="prefix">File prefix</param>
        /// <param name="data"></param>
        /// <param name="writer">Log writer for logging errors and debug information</param>
        public bool WritePrefabReflected<T>(PrefabIdentifier? prefabID, string prefix, T data, LogWriter writer)
        {
            var subWriter = writer.Tag($"IO");
            if (prefabID == null)
            {
                writer.Error($"PrefabIdentifier is null, cannot write: {prefix}");
                return false;
            }
            string fname = $"{prefix}-{prefabID.Id}";
            string json;
            try
            {
                json = JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            catch (Exception e)
            {
                writer.Error($"Failed to serialize json data for file {fname}", e);
                return false;
            }

            return WriteJson($"{prefix}-{prefabID.Id}", json, writer);
        }



        /// <summary>
        /// Writes data associated with a prefab identifier to a JSON file.
        /// </summary>
        /// <param name="prefabID"></param>
        /// <param name="prefix">File prefix</param>
        /// <param name="data"></param>
        /// <param name="writer">Log writer for logging errors and debug information</param>
        public bool WritePrefabData(PrefabIdentifier? prefabID, string prefix, Data data, LogWriter writer)
        {
            var subWriter = writer.Tag($"IO");
            if (prefabID == null)
            {
                writer.Error($"PrefabIdentifier is null, cannot write: {prefix}");
                return false;
            }

            string fname = $"{prefix}-{prefabID.Id}";
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


            return WriteJson(fname, json, writer);
        }

        internal bool WriteReflected<T>(string filename, T data, LogWriter writer)
        {
            string json;
            try
            {
                json = JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            catch (Exception e)
            {
                writer.Error($"Failed to serialize json data for file {filename}", e);
                return false;
            }
            return WriteJson(filename, json, writer);
        }


        internal bool ReadReflected<T>(string filename, out T? data, LogWriter writer) where T : class
        {
            data = ReadJson<T>(filename, writer);
            return data != null;
        }


        /// <summary>
        /// Deserializes JSON data for a specified prefab identifier.
        /// </summary>
        /// <typeparam name="T">Type to deserialize from JSON</typeparam>
        /// <param name="prefabID">The identifier of the prefab to read. Cannot be <see langword="null"/>.</param>
        /// <param name="prefix">A string prefix used to construct the JSON file name.</param>
        /// <param name="outData">Deserialized data</param>
        /// <param name="writer">Log writer</param>
        /// <returns>True on success</returns>
        public bool ReadPrefabReflected<T>(
            PrefabIdentifier? prefabID,
            string prefix,
            [NotNullWhen(true)] out T? outData,
            LogWriter writer) where T : class
        {
            var subWriter = writer.Tag($"IO");
            if (prefabID == null)
            {
                writer.Error($"PrefabIdentifier is null, cannot read: {prefix}");
                outData = null;
                return false;
            }
            outData = ReadJson<T>($"{prefix}-{prefabID.Id}", writer);
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
        /// <param name="writer">The log writer used to record operation details and errors.</param>
        public bool ReadPrefabData(PrefabIdentifier? prefabID, string prefix, Data data, LogWriter writer)
        {
            var subWriter = writer.Tag($"IO");
            if (prefabID == null)
            {
                writer.Error($"PrefabIdentifier is null, cannot read: {prefix}");
                return false;
            }
            var json = ReadJson<JObject>($"{prefix}-{prefabID.Id}", writer);
            if (json != null)
            {
                data.FromJson(json, writer);
                return true;
            }
            else
            {
                writer.Error($"Failed to read JSON for {prefix}-{prefabID.Id}");
                return false;
            }
        }


        //public static T? Read<T>(Component comp, string fileTitle) where T : class
        //    => Read(typeof(T), comp, fileTitle) as T;
        //public static object? Read(Type type, Component comp, string fileTitle)
        //{
        //    if (comp == null)
        //    {
        //        Logger.Error($"Could not perform JsonInterface.Read because comp was null: {fileTitle}");
        //        return default;
        //    }
        //    PrefabIdentifier prefabID = comp.GetComponent<PrefabIdentifier>();
        //    if (prefabID == null)
        //    {
        //        Logger.Error($"Could not perform JsonInterface.Read because comp had no PrefabIdentifier: {fileTitle}");
        //        return default;
        //    }
        //    return Read(type, $"{fileTitle}-{prefabID.Id}");
        //}
        private bool WriteJson(string filename, string json, LogWriter writer)
        {
            List<FilePath> files = new List<FilePath>();
            try
            {
                files.AddRange(GetFiles(filename));
            }
            catch (Exception e)
            {
                writer.Error($"Failed to convert filename '{filename}' to files", e);
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



            string fileContent = $"{{\"hash\":\"{base64Hash}\",\"data\":{json}}}";
            if (fileContent.Length > 100_000_000) // ~100 MB
            {
                writer.Error($"File content is too large ({fileContent.Length} bytes), skipping.");
                return false;
            }

            int written = 0;
            foreach (var file in files)
            {
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
            }
            return written > 0;
        }


        private T? ReadJson<T>(string filename, LogWriter writer) where T : class
        {
            List<FilePath> files = new List<FilePath>();
            try
            {
                files.AddRange(GetFiles(filename));
            }
            catch (Exception e)
            {
                writer.Error($"Failed to convert filename '{filename}' to files in slot {Slot}", e);
                return null;
            }

            foreach (var file in files)
            {
                if (!file.IsFile)
                {
                    writer.Debug($"File does not exist: {file.FullName}");
                    continue;
                }
                if (file.FileSize > 100_000_000) // ~100 MB
                {
                    writer.Error($"File {file.FullName} is too large ({file.FileSize} bytes), skipping.");
                    continue;
                }
                try
                {
                    string json = file.ReadAllText();
                    // Manually decode: {"hash":"...","data":...}
                    int hashIdx = json.IndexOf("\"hash\"");
                    int dataIdx = json.IndexOf("\"data\"");
                    if (hashIdx == -1 || dataIdx == -1)
                    {
                        writer.Error($"File {file.FullName} does not contain 'hash' or 'data' fields.");
                        continue;
                    }
                    int hashStart = json.IndexOf(':', hashIdx) + 1;
                    int hashQuote1 = json.IndexOf('"', hashStart);
                    int hashQuote2 = json.IndexOf('"', hashQuote1 + 1);
                    string hashToken = json.Substring(hashQuote1 + 1, hashQuote2 - hashQuote1 - 1);

                    int dataStart = json.IndexOf(':', dataIdx) + 1;
                    // Find the start of the data (skip whitespace)
                    while (dataStart < json.Length && char.IsWhiteSpace(json[dataStart])) dataStart++;
                    // Data can be any JSON value (object, array, string, etc.)
                    // We assume it's an object or array, so we find the matching bracket
                    int dataEnd = json.LastIndexOf('}');
                    if (dataEnd <= dataStart)
                    {
                        writer.Error($"Malformed JSON in file {file.FullName} (data field).");
                        continue;
                    }
                    // The data field may be followed by a closing } (end of root object)
                    // So we want to extract from dataStart to dataEnd (exclusive of last })
                    string dataJson = json.Substring(dataStart, dataEnd - dataStart).TrimEnd();
                    // Remove trailing comma if present
                    if (dataJson.EndsWith(","))
                        dataJson = dataJson.Substring(0, dataJson.Length - 1);

                    using var sha256 = SHA256.Create();
                    var hashedJson = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(dataJson));
                    string base64Hash = Convert.ToBase64String(hashedJson);
                    if (base64Hash == hashToken)
                    {
                        // Parse dataJson as JToken
                        var rs = JsonConvert.DeserializeObject<T>(dataJson) ?? throw new JsonSerializationException($"Failed to deserialize data for {filename}");
                        writer.Debug($"Successfully read file {file.FullName} with length {dataJson.Length}");
                        return rs;
                    }
                    else
                    {
                        writer.Error($"Hash mismatch for file {file.FullName}. Expected: {hashToken}, Actual: {base64Hash}");
                    }
                }
                catch (Exception e)
                {
                    writer.Error($"Failed to read or parse file {file.FullName}", e);
                }
            }
            writer.Error($"No valid JSON data found in files for {filename} in slot {Slot}");
            return null;
        }


        private IEnumerable<FilePath> GetFiles(string innerName)
        {
            string directoryPath = Path.Combine(PlatformServicesNull.DefaultSavePath, Slot);
            string configFolderPath = Path.Combine(directoryPath, SaveFolderName);
            if (!Directory.Exists(configFolderPath))
            {
                Directory.CreateDirectory(configFolderPath);
            }
            yield return new FilePath(configFolderPath, $"{innerName}.json");
            yield return new FilePath(configFolderPath, $"{innerName}-fb.json");
        }

        private FileInfo ToFile(string innerName)
        {
            string directoryPath = Path.Combine(PlatformServicesNull.DefaultSavePath, Slot);
            string configFolderPath = Path.Combine(directoryPath, SaveFolderName);
            if (!Directory.Exists(configFolderPath))
            {
                Directory.CreateDirectory(configFolderPath);
            }
            return new FileInfo(Path.Combine(configFolderPath, $"{innerName}.json"));
        }

    }
}
