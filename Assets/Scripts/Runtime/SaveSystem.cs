using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

public static class SaveSystem
{
    public class SaveGameMetadata
    {
        public string systemName;
        public int simTick;
        // health, progress, whatever
    }

    public class SaveGameData
    {
        public Map map;
        public SolarSystem system;
        public Vector3 playerPosition;
        public Quaternion playerRotation;
        public Vector3 playerVelocity;
        public int simTick;
        public List<(string name, object data)> installedUpgrades;
    }

    public static async Task<bool> SaveExistsAsync(int index) => await FileExistsAsync(GetSaveMetaFilePath(index));

    public static async Task SaveAsync(int index, SaveGameData data)
    {
        string metadataPath = GetSaveMetaFilePath(index);
        if (await FileExistsAsync(metadataPath))
        {
            await DeleteFileAsync(metadataPath);
        }
        await SaveObjectAsync(metadataPath, new SaveGameMetadata {
            systemName = data.system.name,
            simTick = data.simTick,
        });

        string path = GetSaveFilePath(index);
        if (await FileExistsAsync(path))
        {
            await DeleteFileAsync(path);
        }
        await SaveObjectAsync(path, data);
    }

    public static async Task<SaveGameMetadata> LoadMetadataAsync(int index)
    {
        string path = GetSaveMetaFilePath(index);
        if (await FileExistsAsync(path))
        {
            try
            {
                return await LoadObjectAsync<SaveGameMetadata>(path);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }
        return null;
    }

    public static async Task<SaveGameData> LoadAsync(int index)
    {
        string path = GetSaveFilePath(index);
        if (await FileExistsAsync(path))
        {
            try
            {
                return await LoadObjectAsync<SaveGameData>(path);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }
        return null;
    }

    public static async Task DeleteAsync(int index)
    {
        await DeleteFileAsync(GetSaveMetaFilePath(index));
        await DeleteFileAsync(GetSaveFilePath(index));
    }

    #region Save Helpers
    static string GetSaveMetaFilePath(int index) => Path.Combine(Application.persistentDataPath, $"save{index}.meta.xml");
    static string GetSaveFilePath(int index) => Path.Combine(Application.persistentDataPath, $"save{index}.xml");

    static async Task<bool> FileExistsAsync(string path) => await Task.Run(() => File.Exists(path));

    static async Task DeleteFileAsync(string path) => await Task.Run(() => File.Delete(path));

    static async Task<T> LoadObjectAsync<T>(string path)
    {
        return await Task.Run(() =>
        {
            using (var ms = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var bf = new DataContractSerializer(typeof(T));
                return (T)bf.ReadObject(ms);
            }
        });
    }

    static async Task SaveObjectAsync<T>(string path, T obj)
    {
        await Task.Run(() =>
        {
            var settings = new XmlWriterSettings { Indent = true };
            // using (var ms = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            using (var xmlWriter = XmlWriter.Create(path, settings))
            {
                var dcsSettings = new DataContractSerializerSettings
                {
                    PreserveObjectReferences = true
                };
                var bf = new DataContractSerializer(typeof(T), dcsSettings);
                bf.WriteObject(xmlWriter, obj);
            }
        });
    }
    #endregion
}
