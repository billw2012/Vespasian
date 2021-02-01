using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Attribute = System.Attribute;

// This might be useful to solve problems with order of init on load, but not necessary yet.
//
// public enum LoadOrder
// {
//     Any,
//     First,
//     PrePostLoad,  Seems weird. Can we explicitly specify types in ordering? Or just use a number for priority?
//     PostPostLoad,
//     Last
// }
//
// [AttributeUsage(AttributeTargets.Class)]
// public class LoadOrderAttribute : Attribute 
// {
//     public LoadOrder loadOrder;
//
//     public LoadOrderAttribute(LoadOrder loadOrder) { this.loadOrder = loadOrder; }
// }

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
/// Optionally used to provide custom pre-save code for a component. This is called before any fields are saved,
/// allowing the object to do any updates that might be required.
/// ISavable is still required, and any use of the [Saved] attribute is still respected. These functions are called
/// after the [Saved] fields and properties are dealt with. They should NOT do any slow blocking operations, these
/// should be done using IPostLoadAsync instead (obviously in a none blocking manner as well).
/// </summary>
public interface IPreSave
{
    void PreSave();
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
[RegisterSavableType]
public class SaveData : ISaver, ILoader
{
    public DictX<string, object> data = new DictX<string, object>();

    public static SaveData SaveObject(ISavable obj)
    {
        if (obj is IPreSave preSave)
        {
            preSave.PreSave();
        }

        var data = new SaveData();
        if (obj != null)
        {
            ForEachField(obj, f => data.SaveValue(f.Name, f.GetValue(obj)));
            ForEachProperty(obj, f => data.data.Add(f.Name, f.GetValue(obj)));
        }

        if (obj is ISavableCustom savableCustom)
        {
            savableCustom.Save(data);
        }
        return data;
    }

    public static void LoadObject(ISavable obj, SaveData data)
    {
        if (obj != null)
        {
            ForEachField(obj, f => f.SetValue(obj, data.LoadValue<object>(f.Name)));
            ForEachProperty(obj, f => f.SetValue(obj, data.LoadValue<object>(f.Name)));
        }

        if (obj is ISavableCustom savableCustom)
        {
            savableCustom.Load(data);
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

    private static void ForEachField(object obj, Action<FieldInfo> op)
    {
        const BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        foreach (var field in obj.GetType()
            .GetFields(flag)
            .Where(f => f.CustomAttributes.Any(a => a.AttributeType == typeof(SavedAttribute))))
        {
            op(field);
        }
    }

    private static void ForEachProperty(object obj, Action<PropertyInfo> op)
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

    private readonly Dictionary<string, ISavable> saveables = new Dictionary<string, ISavable>();
    private IEnumerable<Type> knownTypes;

    private void Awake()
    {
        const BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        
        float startTime = Time.realtimeSinceStartup;
        // Find all registered savable types
        var allTypes = Assembly.GetExecutingAssembly().GetTypes();
        this.knownTypes = allTypes
            // All the class and struct attributes
            .Select(t => (type: t, attr: t.GetCustomAttributes(typeof(RegisterSavableTypeAttribute))?.OfType<RegisterSavableTypeAttribute>(), inherit: true))
            // All properties
            .Concat(allTypes
                .SelectMany(t => t.GetProperties(flag)
                    .Select(p => (type: p.PropertyType, attr: p.GetCustomAttributes(typeof(RegisterSavableTypeAttribute))?.OfType<RegisterSavableTypeAttribute>(), inherit: true))
                )
            )
            // All fields
            .Concat(allTypes
                .SelectMany(t => t.GetFields(flag)
                    .Select(f => (type: f.FieldType, attr: f.GetCustomAttributes(typeof(RegisterSavableTypeAttribute))?.OfType<RegisterSavableTypeAttribute>(), inherit: true))
                )
            )
            // All methods
            .Concat(allTypes
                .SelectMany(t => t.GetMethods(flag)
                    .Select(m => (type: default(Type), attr: m.GetCustomAttributes(typeof(RegisterSavableTypeAttribute))?.OfType<RegisterSavableTypeAttribute>(), inherit: true))
                )
            )
            .Where(v => v.attr?.Any() ?? false)
            .SelectMany(v => v.attr.Select(a => a.type ?? v.type))
            .Distinct()
            .Where(t => t != null)
            .ToList()
            ;
        // var allTypeNames = string.Join("\n  ", this.knownTypes.Select(t => t.Name));
        Debug.Log($"{Time.realtimeSinceStartup - startTime}s to enumerate all Savable types:\n{string.Join("\n", this.knownTypes.Select(t => t.ToString()))}");
    }

    /// <summary>
    /// Call this to register a static scene object / component for saving. ONLY registered objects will be saved,
    /// unless they are handled by a custom save function of a registered object.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="savable"></param>
    public void RegisterForSaving(string id, ISavable savable) => this.saveables.Add(id, savable);

    /// <summary>
    /// Call this for registering a static scene object / component for saving,
    /// using its type name as the key automatically (this implies it is a singleton).
    /// ONLY registered objects will be saved, unless they are handled by a custom save
    /// function of a registered object.
    /// </summary>
    /// <param name="savable"></param>
    public void RegisterForSaving(ISavable savable) => this.saveables.Add(savable.GetType().ToString(), savable);

    /// <summary>
    /// Check if save at index exists. Doesn't guarantee it can be loaded though...
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public async Task<bool> SaveExistsAsync(int index) => await FileExistsAsync(GetSaveMetaFilePath(index));

    // #region Save Wrappers
    // // These types are workarounds to get IL2cpp to work with the save system
    // [RegisterSavableType]
    // public class DictionaryEntryWrapper
    // {
    //     public object Key;
    //     public object Value;
    //
    //     public DictionaryEntryWrapper() {}
    //     public DictionaryEntryWrapper(DictionaryEntry from)
    //     {
    //         this.Key = from.Key;
    //         this.Value = from.Value;
    //     }
    // }
    //
    // [RegisterSavableType]
    // public class DictionaryWrapper : List<DictionaryEntryWrapper>
    // {
    //     public DictionaryWrapper() { }
    //     public DictionaryWrapper(IEnumerable<DictionaryEntryWrapper> collection) : base(collection) { }
    // }
    //
    // public class DictionarySurrogate : ISerializationSurrogateProvider //IDataContractSurrogate
    // {
    //     public object GetObjectToSerialize(object obj, Type targetType)
    //     {
    //         // Debug.Log($"{obj.GetType().Name} == {targetType.Name}");
    //         // Look for any Dictionary<> regardless of generic parameters
    //         var type = obj.GetType();
    //         if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
    //         {
    //             var wrapper = new DictionaryWrapper();
    //             foreach (DictionaryEntry de in (IDictionary)obj)
    //             {
    //                 wrapper.Add(new DictionaryEntryWrapper(de));
    //             }
    //             return wrapper;
    //         }
    //         return obj;
    //     }
    //     
    //     public object GetDeserializedObject(object obj, Type targetType)
    //     {
    //         var type = obj.GetType();
    //         if(type == typeof(DictionaryWrapper))
    //         {
    //             // We can just use the non-generic interface, which makes things a lot easier
    //             var target = (IDictionary)Activator.CreateInstance(targetType);
    //             foreach (var kv in (DictionaryWrapper)obj)
    //             {
    //                 target.Add(kv.Key, kv.Value);
    //             }
    //             return target;
    //         }
    //         return obj;
    //     }
    //
    //     public Type GetSurrogateType(Type type)
    //     {
    //         // Look for any Dictionary<> regardless of generic parameters, then return a DictionaryWrapper
    //         if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
    //         {
    //             return typeof(DictionaryWrapper);
    //         }
    //         
    //         return type;
    //     }
    // }
    // #endregion Save Wrappers
    
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
        var simManager = FindObjectOfType<Simulation>();
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
        var data = new DictX<string, SaveData>(this.saveables
            .ToDictionary(
                kv => kv.Key,
                kv => SaveData.SaveObject(kv.Value))
            );

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
                var data = await this.DeserializeObjectAsync<DictX<string, SaveData>>(path);

                // Restore all registered savable objects
                foreach (var savable in this.saveables)
                {
                    if (data.TryGetValue(savable.Key, out var savedata))
                    {
                        try
                        {
                            SaveData.LoadObject(savable.Value, savedata);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Failed to load object '{savable.Key}' ({ex.Message})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return false;
            }

            // Trigger post load behaviours
            foreach(var postLoadSavable in this.saveables.Values.OfType<IPostLoadAsync>())
            {
                await postLoadSavable.OnPostLoadAsync();
            }

            return true;

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

    public static void SaveComponents(ISaver saver, string prefix, GameObject go) =>
        SaveComponents(saver, prefix, go.GetComponents<ISavable>());

    public static void SaveComponents(ISaver saver, string prefix, IEnumerable<ISavable> savables)
    {
        foreach (var s in savables)
        {
            saver.SaveObject($"{prefix}.{s.GetType().Name}", s);   
        }
    }

    public static void LoadComponents(ILoader loader, string prefix, GameObject go) => 
        LoadComponents(loader, prefix, go.GetComponents<ISavable>());
    
    public static void LoadComponents(ILoader loader, string prefix, IEnumerable<ISavable> savables)
    {
        foreach (var s in savables)
        {
            loader.LoadObject($"{prefix}.{s.GetType().Name}", s);   
        }
    }
    
    #region Save Helpers

    private static string GetSaveMetaFilePath(int index) => Path.Combine(Application.persistentDataPath, $"save{index}.meta.xml");
    private static string GetSaveFilePath(int index) => Path.Combine(Application.persistentDataPath, $"save{index}.xml");

    private static async Task<bool> FileExistsAsync(string path) => await TaskX.Run(() => File.Exists(path));

    private static async Task DeleteFileAsync(string path) => await TaskX.Run(() => File.Delete(path));

    private async Task<T> DeserializeObjectAsync<T>(string path)
    {
        return await TaskX.Run(() =>
        {
            using (var ms = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var dcsSettings = new DataContractSerializerSettings {
                    PreserveObjectReferences = false,
                    KnownTypes = this.knownTypes,
                };
                var bf = new DataContractSerializer(typeof(T), dcsSettings);
// #if !UNITY_WEBGL
//                 bf.SetSerializationSurrogateProvider(new DictionarySurrogate());
// #endif
                return (T)bf.ReadObject(ms);
            }
        });
    }

    private async Task SerializeObjectAsync<T>(string path, T obj)
    {
        await TaskX.Run(() =>
        {
            var settings = new XmlWriterSettings { Indent = true };
            using (var xmlWriter = XmlWriter.Create(path, settings))
            {
                var dcsSettings = new DataContractSerializerSettings {
                    PreserveObjectReferences = false,
                    KnownTypes = this.knownTypes,
                };
                var bf = new DataContractSerializer(typeof(T), dcsSettings);
// #if !UNITY_WEBGL
//                 bf.SetSerializationSurrogateProvider(new DictionarySurrogate());
// #endif
                bf.WriteObject(xmlWriter, obj);
            }
        });
    }
    #endregion
}
