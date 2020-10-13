using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu]
public class GameLogic : ScriptableObject {
    GUILayerManager uiManager;
    MapComponent mapComponent;
    PlayerController player;

    int saveIndex;

    public class SaveGameData
    {
        public Map map;
        public SolarSystem system;
        public Vector3 playerPosition;
        public Quaternion playerRotation;
        public Vector3 playerVelocity;
        public int simTick;
        //public List<>
    }

    void OnEnable() => SceneManager.sceneLoaded += this.SceneManager_sceneLoaded;

    void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        this.uiManager = FindObjectOfType<GUILayerManager>();
        this.mapComponent = FindObjectOfType<MapComponent>();
        this.player = FindObjectOfType<PlayerController>();

        if(this.mapComponent != null && this.player != null)
        {
            this.uiManager.ShowMainMenuUI();
        }
    }
    async Task NewGameAsync()
    {
        await this.mapComponent.GenerateMapAsync();
        await this.mapComponent.LoadRandomSystemAsync();
        await this.SaveGameAsync();
        //this.uiManager.ShowPlayUI();
    }

    static string GetSaveFilePath(int index) => Path.Combine(Application.persistentDataPath, $"save{index}.xml");

    static async Task<bool> FileExistsAsync(string path) => await Task.Run(() => File.Exists(path));

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
                var dcsSettings = new DataContractSerializerSettings {
                    PreserveObjectReferences = true
                };
                var bf = new DataContractSerializer(typeof(T), dcsSettings);
                bf.WriteObject(xmlWriter, obj);
            }
        });
    }

    public async Task SaveGameAsync()
    {
        string path = GetSaveFilePath(this.saveIndex);

        var playerSimMovement = this.player.gameObject.GetComponent<SimMovement>();
        var simManager = FindObjectOfType<SimManager>();
        var saveData = new SaveGameData
        {
            map = this.mapComponent.map,
            system = this.mapComponent.currentSystem,
            playerPosition = playerSimMovement.transform.position,
            playerRotation = playerSimMovement.transform.rotation,
            playerVelocity = playerSimMovement.velocity,
            simTick = simManager.simTick,
        };
        await SaveObjectAsync(path, saveData);
    }

    public async void LoadGameAsync(int index)
    {
        this.saveIndex = index;

        string path = GetSaveFilePath(index);
        if (await FileExistsAsync(path))
        {
            try
            {
                var saveData = await LoadObjectAsync<SaveGameData>(path);

                var playerSimMovement = this.player.gameObject.GetComponent<SimMovement>();
                var simManager = FindObjectOfType<SimManager>();

                this.mapComponent.map = saveData.map;
                simManager.simTick = saveData.simTick;
                playerSimMovement.SetPositionVelocity(saveData.playerPosition, saveData.playerRotation, saveData.playerVelocity);
                await this.mapComponent.LoadSystemAsync(saveData.system);
                this.uiManager.ShowPlayUI();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message, this);
            }
        }
        else
        {
            await this.NewGameAsync();
        }

        //var txt = File.ReadAllText(GetSaveFilePath(index));
    }


    public bool CanJump() => this.mapComponent.CanJump();

    public async void JumpAsync()
    {
        await this.mapComponent.JumpAsyc();
        await this.SaveGameAsync();
    }

    public void RestartLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    public void OpenMap() => this.uiManager.ShowMapUI();

    public void CloseMap() => this.uiManager.ShowPlayUI();

    public void LoseGame()
    {
        this.uiManager.ShowLoseUI();

        this.player.gameObject.SetActive(false);
    }
}