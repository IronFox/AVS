﻿using Newtonsoft.Json;
using System;
using System.IO;
using UnityEngine;

namespace AVS.SaveLoad
{
    public static class JsonInterface
    {
        public static string SaveFolderName { get; } = "AVSSaveData";
        public static void Write<T>(Component comp, string fileTitle, T data)
        {
            if (comp == null)
            {
                Logger.Error($"Could not perform JsonInterface.Write because comp was null: {fileTitle}");
                return;
            }
            PrefabIdentifier prefabID = comp.GetComponent<PrefabIdentifier>();
            if (prefabID == null)
            {
                Logger.Error($"Could not perform JsonInterface.Write because comp had no PrefabIdentifier: {fileTitle}");
                return;
            }
            Write<T>($"{fileTitle}-{prefabID.Id}", data);
        }
        public static T? Read<T>(Component comp, string fileTitle) where T : class
            => Read(typeof(T), comp, fileTitle) as T;
        public static object? Read(Type type, Component comp, string fileTitle)
        {
            if (comp == null)
            {
                Logger.Error($"Could not perform JsonInterface.Read because comp was null: {fileTitle}");
                return default;
            }
            PrefabIdentifier prefabID = comp.GetComponent<PrefabIdentifier>();
            if (prefabID == null)
            {
                Logger.Error($"Could not perform JsonInterface.Read because comp had no PrefabIdentifier: {fileTitle}");
                return default;
            }
            return Read(type, $"{fileTitle}-{prefabID.Id}");
        }
        public static void Write<T>(string uniqueFileName, T data)
        {
            string fileName;
            try
            {
                fileName = GetFilePath(uniqueFileName);
            }
            catch (Exception e)
            {
                Logger.LogException("Failed to GetFilePath!", e);
                return;
            }
            string json;
            try
            {
                json = JsonConvert.SerializeObject(data, Formatting.Indented);
            }
            catch (Exception e)
            {
                Logger.LogException($"Failed to serialize json data for file {uniqueFileName}!", e);
                return;
            }
            try
            {
                if (fileName.Length > 260)
                {
                    throw new ArgumentException("That file path was too long!");
                }
                File.WriteAllText(fileName, json);
            }
            catch (Exception e)
            {
                Logger.LogException($"Failed to write file {uniqueFileName}!", e);
            }
        }
        public static T? Read<T>(string uniqueFileName) where T : class
            => Read(typeof(T), uniqueFileName) as T;
        public static object? Read(Type type, string uniqueFileName)
        {
            string fileName;
            try
            {
                fileName = GetFilePath(uniqueFileName);
            }
            catch (Exception e)
            {
                Logger.LogException("Failed to GetFilePath!", e);
                return default;
            }
            if (!File.Exists(fileName))
            {
                Logger.Log($"File does not exist: {fileName}");
                return default;
            }


            //SHA256 sha256 = SHA256.Create();

            string json;
            try
            {
                json = File.ReadAllText(fileName);
                //var hashedJson = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(json));
            }
            catch (Exception e)
            {
                Logger.LogException($"Failed to read file {uniqueFileName}!", e);
                return default;
            }
            try
            {
                return JsonConvert.DeserializeObject(json,type);
            }
            catch (Exception e)
            {
                Logger.LogException($"Failed to deserialize json from file {uniqueFileName}!", e);
            }
            return default;
        }
        private static string GetFilePath(string innerName)
        {
            string directoryPath = Path.Combine(PlatformServicesNull.DefaultSavePath, SaveLoadManager.main.GetCurrentSlot());
            string configFolderPath = Path.Combine(directoryPath, SaveFolderName);
            if (!Directory.Exists(configFolderPath))
            {
                Directory.CreateDirectory(configFolderPath);
            }
            return Path.Combine(configFolderPath, $"{innerName}.json");
        }
    }
}
