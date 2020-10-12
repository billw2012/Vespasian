using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu]
public class GameLogic : ScriptableObject {
    GUILayerManager uiManager;
    MapComponent mapComponent;
    PlayerController player;

    int saveIndex;

    public struct SaveGameData
    {
        public Map map;
        //public List<>
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += this.SceneManager_sceneLoaded;
    }

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
        //this.uiManager.ShowPlayUI();
    }

    static string GetSaveFilePath(int index) => Path.Combine(Application.persistentDataPath, $"save{index}.xml");

    public async void LoadGameAsync(int index)
    {
        this.saveIndex = index;

        string path = GetSaveFilePath(index);
        if (File.Exists(path))
        {
            using (var ms = new FileStream(path, FileMode.Open))
            {
                //var bf = new DataContractSerializer(o.GetType());
                //bf.WriteObject(ms, o);
            }
        }
        else
        {
            await NewGameAsync();
        }

        //var txt = File.ReadAllText(GetSaveFilePath(index));
    }

    public async void SaveGameAsync()
    {
        string path = GetSaveFilePath(this.saveIndex);
        using (var ms = new FileStream(path, FileMode.OpenOrCreate))
        {
            //var bf = new DataContractSerializer(o.GetType());
            //bf.WriteObject(ms, o);
        }
    }

    public bool CanJump()
    {
        return this.mapComponent.CanJump();
    }

    public void Jump()
    {
        _ = this.mapComponent.JumpAsyc();
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OpenMap()
    {
        this.uiManager.ShowMapUI();
    }

    public void CloseMap()
    {
        this.uiManager.ShowPlayUI();
    }

    public void LoseGame()
    {
        this.uiManager.ShowLoseUI();

        this.player.gameObject.SetActive(false);
    }
}