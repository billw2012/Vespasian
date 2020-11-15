﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

/// <summary>
/// Required on a type to indicate that it can be saved using this system
/// </summary>
public interface ISavable {}

/// <summary>
/// Interface that defines how values and objects can be saved
/// </summary>
public interface ISaver
{
    void SaveValue(string key, object value);
    void SaveObject(string key, ISavable obj);
}

/// <summary>
/// Interface that defines how values and objects can be loaded
/// </summary>
public interface ILoader
{
    T LoadValue<T>(string key);
    void LoadObject(string key, ISavable obj);
}

/// <summary>
/// Optionally used to provide custom save/load code for a component. ISavable is still required, and 
/// any use of the [Saved] attribute is still respected. These functions are called after the [Saved] 
/// fields and properties are dealt with. They should NOT do any slow blocking operations, these should be done
/// using IPostLoadAsync instead (obviously in a none blocking manner as well).
/// </summary>
public interface ISavableCustom
{
    void Save(ISaver saver);
    void Load(ILoader loader);
}

/// <summary>
/// Implement this to optionally provide custom post load behaviour. This will be called once ALL 
/// registered ISaveables are loaded
/// </summary>
public interface IPostLoadAsync
{
    Task OnPostLoadAsync();
}

/// <summary>
/// Apply to any public field or property to save/load it automatically, requires ISaved
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SavedAttribute : Attribute { }

/// <summary>
/// Used to register any non-basic types that will be saved. Can be used on a 
/// property, field, class or struct without specifying the type explicitly, 
/// or on a method while specifying type explicitly, allowing registration 
/// of types used within the function (or anywhere really, the registration is globally applicable).
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = true)]
public class RegisterSavableTypeAttribute : Attribute 
{
    public Type type;

    public RegisterSavableTypeAttribute(Type type = null) { this.type = type; }
}

/// <summary>
/// A simple dictionary based save/load implementation.
/// </summary>
[KnownType(typeof(SaveData))]
public class SaveData : ISaver, ILoader
{
    public Dictionary<string, object> data = new Dictionary<string, object>();

    public static SaveData SaveObject(ISavable obj)
    {
        var data = new SaveData();
        if (obj is ISavable)
        {
            ForEachField(obj, f => data.SaveValue(f.Name, f.GetValue(obj)));
            ForEachProperty(obj, f => data.data.Add(f.Name, f.GetValue(obj)));
        }

        if (obj is ISavableCustom)
        {
            (obj as ISavableCustom).Save(data);
        }
        return data;
    }

    public static void LoadObject(ISavable obj, SaveData data)
    {
        if (obj is ISavable)
        {
            ForEachField(obj, f => f.SetValue(obj, data.LoadValue<object>(f.Name)));
            ForEachProperty(obj, f => f.SetValue(obj, data.LoadValue<object>(f.Name)));
        }

        if (obj is ISavableCustom)
        {
            (obj as ISavableCustom).Load(data);
        }
    }

    #region ISaver
    public void SaveValue(string key, object value) => this.data.Add(key, value);
    public void SaveObject(string key, ISavable value) => this.data.Add(key, SaveData.SaveObject(value));
    #endregion ISaver

    #region ILoader
    public T LoadValue<T>(string key) => (T)this.data[key];
    public void LoadObject(string key, ISavable value) => SaveData.LoadObject(value, this.data[key] as SaveData);
    #endregion ILoader

    static void ForEachField(object obj, Action<FieldInfo> op)
    {
        const BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        foreach (var field in obj.GetType()
            .GetFields(flag)
            .Where(f => f.CustomAttributes.Any(a => a.AttributeType == typeof(SavedAttribute))))
        {
            op(field);
        }
    }
    static void ForEachProperty(object obj, Action<PropertyInfo> op)
    {
        const BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        foreach (var property in obj.GetType()
            .GetProperties(flag)
            .Where(f => f.CustomAttributes.Any(a => a.AttributeType == typeof(SavedAttribute))))
        {
            op(property);
        }
    }

}

/// <summary>
/// Object through which save/load operations are performed (instance it in the scene somewhere).
/// It relies on objects registering themselves with this system. It can not instance new Unity objects itself,
/// only save the state of existing ones, then restore it later. Any dynamic objects (upgrades, missions, etc.) should be handled by 
/// custom serialization in a registered static object.
/// This custom serialization is encouraged to use the SaveData system as well internally.
/// </summary>
public class SaveSystem : MonoBehaviour
{
    public class SaveGameMetadata
    {
        public string systemName;
        public int simTick;
        // health, progress, whatever
    }

    readonly Dictionary<string, ISavable> saveables = new Dictionary<string, ISavable>();
    IEnumerable<Type> knownTypes;

    void Awake()
    {
        float startTime = Time.realtimeSinceStartup;
        // Find all registered savable types
        var allTypes = Assembly.GetExecutingAssembly().GetTypes();
        this.knownTypes = allTypes
            // All the class and struct attributes
            .Select(t => (type: t, attr: t.GetCustomAttributes(typeof(RegisterSavableTypeAttribute))?.OfType<RegisterSavableTypeAttribute>(), inherit: true))
            // All properties
            .Concat(allTypes
                .SelectMany(t => t.GetProperties()
                    .Select(p => (type: p.PropertyType, attr: p.GetCustomAttributes(typeof(RegisterSavableTypeAttribute))?.OfType<RegisterSavableTypeAttribute>(), inherit: true))
                )
            )
            // All fields
            .Concat(allTypes
                .SelectMany(t => t.GetFields()
                    .Select(f => (type: f.FieldType, attr: f.GetCustomAttributes(typeof(RegisterSavableTypeAttribute))?.OfType<RegisterSavableTypeAttribute>(), inherit: true))
                )
            )
            // All methods
            .Concat(allTypes
                .SelectMany(t => t.GetMethods()
                    .Select(m => (type: default(Type), attr: m.GetCustomAttributes(typeof(RegisterSavableTypeAttribute))?.OfType<RegisterSavableTypeAttribute>(), inherit: true))
                )
            )
            .Where(v => v.attr?.Any() ?? false)
            .SelectMany(v => v.attr.Select(a => a.type ?? v.type))
            .Distinct()
            .Where(t => t != null)
            .ToList()
            ;
        Debug.Log($"{Time.realtimeSinceStartup - startTime}s to enumerate all Savable types");
    }

    /// <summary>
    /// Call this to register a static scene object / component for saving. ONLY registered objects will be saved,
    /// unless they are handled by a custom save function of a registered object.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="savable"></param>
    public void RegisterForSaving(string id, ISavable savable) => this.saveables.Add(id, savable);

    /// <summary>
    /// Check if save at index exists. Doesn't guarantee it can be loaded though...
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public async Task<bool> SaveExistsAsync(int index) => await FileExistsAsync(GetSaveMetaFilePath(index));

    /// <summary>
    /// Save to slot at index, overwriting anything present
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public async Task SaveAsync(int index)
    {
        // Write out the save meta data, replacing any existing one
        string metadataPath = GetSaveMetaFilePath(index);
        if (await FileExistsAsync(metadataPath))
        {
            await DeleteFileAsync(metadataPath);
        }
        var simManager = FindObjectOfType<SimManager>();
        var mapComponent = FindObjectOfType<MapComponent>();

        await this.SerializeObjectAsync(metadataPath, new SaveGameMetadata {
            systemName = mapComponent.currentSystem.name,
            simTick = simManager.simTick,
        });

        // Delete any existing save file
        string path = GetSaveFilePath(index);
        if (await FileExistsAsync(path))
        {
            await DeleteFileAsync(path);
        }

        // Save all registered objects into our save data structures
        var data = this.saveables.ToDictionary(kv => kv.Key, kv => SaveData.SaveObject(kv.Value));

        // Write out the save data structures to the file
        await this.SerializeObjectAsync(path, data);
    }

    /// <summary>
    /// Load save metadata from specified index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public async Task<SaveGameMetadata> LoadMetadataAsync(int index)
    {
        string path = GetSaveMetaFilePath(index);
        if (await FileExistsAsync(path))
        {
            try
            {
                return await this.DeserializeObjectAsync<SaveGameMetadata>(path);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }
        return null;
    }

    /// <summary>
    /// Load save at index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public async Task<bool> LoadAsync(int index)
    {
        string path = GetSaveFilePath(index);
        if (await FileExistsAsync(path))
        {
            try
            {
                // Load the save data structure from the file
                var data = await this.DeserializeObjectAsync<Dictionary<string, SaveData>>(path);

                // Restore all registered savable objects
                foreach (var savable in this.saveables)
                {
                    if (data.TryGetValue(savable.Key, out var savedata))
                    {
                        SaveData.LoadObject(savable.Value, savedata);
                    }
                }

                // Trigger post load behaviours
                foreach(var postLoadSavable in this.saveables.Values.OfType<IPostLoadAsync>())
                {
                    await postLoadSavable.OnPostLoadAsync();
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Delete save at index, if it exists
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
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

    async Task<T> DeserializeObjectAsync<T>(string path)
    {
        return await Task.Run(() =>
        {
            using (var ms = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var bf = new DataContractSerializer(typeof(T), this.knownTypes);
                return (T)bf.ReadObject(ms);
            }
        });
    }

    async Task SerializeObjectAsync<T>(string path, T obj)
    {
        await Task.Run(() =>
        {
            var settings = new XmlWriterSettings { Indent = true };
            // using (var ms = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
            using (var xmlWriter = XmlWriter.Create(path, settings))
            {
                var dcsSettings = new DataContractSerializerSettings {
                    PreserveObjectReferences = true,
                    KnownTypes = this.knownTypes
                };
                var bf = new DataContractSerializer(typeof(T), dcsSettings);
                bf.WriteObject(xmlWriter, obj);
            }
        });
    }
    #endregion
}