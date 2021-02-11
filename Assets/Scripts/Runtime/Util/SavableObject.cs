using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Put on an object to cause its ISavable components to be saved.
/// </summary>
public class SavableObject : MonoBehaviour, ISavable, ISavableCustom
{
    [SerializeField, Tooltip("Whether to also save all child ISavable components, only do this for objects that do not dynamically change their contents")]
    private bool recursive = false;
    [SerializeField, Tooltip("Whether to self register with the SaveManager. Only do this for objects that are part of the scene when it is created.")]
    private bool selfRegister = false;

    [Saved]
    public DictX<string, SaveData> savedComponents;
    
    private IEnumerable<(string id, ISavable savable)> savables;
    
    private void Awake()
    {
        if (this.selfRegister)
        {
            ComponentCache.FindObjectOfType<SaveSystem>().RegisterForSaving(this);
        }
        // We cache these on creation, because this list cannot change dynamically
        // or it wouldn't match save data
        this.savables = (this.recursive
            ? this.GetComponentsInChildren<ISavable>()
            : this.GetComponents<ISavable>())
            .Where(s => !ReferenceEquals(s, this))
            .Select(c => (SaveData.GetRelativeKey(c as MonoBehaviour, this.transform), c))
            .ToList();
    }

    public void SaveComponents()
    {
        this.savedComponents = new DictX<string, SaveData>();
        
        foreach (var (id, savable) in this.savables)
        {
            this.savedComponents.Add(id, SaveData.SaveObject(savable));
        }
    }
    
    public void LoadComponents()
    {
        Assert.IsNotNull(this.savedComponents);
        
        foreach(var (id, savable) in this.savables)
        {
            if(this.savedComponents.TryGetValue(id, out var data))
            {
                SaveData.LoadObject(savable, data);
            }
        }
    }

    public void Save(ISaver saver) => this.SaveComponents();

    public void Load(ILoader loader) => this.LoadComponents();
}