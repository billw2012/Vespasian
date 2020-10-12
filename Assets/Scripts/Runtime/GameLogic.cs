using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu]
public class GameLogic : ScriptableObject {
    GUILayerManager uiManager;
    MapComponent mapComponent;
    PlayerController player;

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

        this.uiManager.ShowMainMenu();
    }

    public void NewGame()
    {
        this.mapComponent.GenerateMap();
    }

    static string GetSaveFilePath(int index) => Path.Combine(Application.persistentDataPath, $"save{index}.xml");

    public void LoadGame(int index)
    {
        using (var ms = new FileStream(GetSaveFilePath(index), FileMode.Open))
        {
            //var bf = new DataContractSerializer(o.GetType());
            //bf.WriteObject(ms, o);
        }

        //var txt = File.ReadAllText(GetSaveFilePath(index));
    }

    public void SaveGame(int index)
    {

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