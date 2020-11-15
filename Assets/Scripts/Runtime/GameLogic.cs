using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu]
public class GameLogic : ScriptableObject {
    GUILayerManager uiManager;

    MapComponent mapComponent;
    PlayerController player;
    SaveSystem saveSystem;

    int saveIndex;

    void OnEnable() => SceneManager.sceneLoaded += this.SceneManager_sceneLoaded;

    void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        this.uiManager = FindObjectOfType<GUILayerManager>();
        this.mapComponent = FindObjectOfType<MapComponent>();
        this.player = FindObjectOfType<PlayerController>();
        this.saveSystem = FindObjectOfType<SaveSystem>();
        if (this.mapComponent != null && this.player != null)
        {
            this.uiManager.ShowMainMenuUI();
        }
    }

    async Task NewGameAsync()
    {
        await this.mapComponent.GenerateMapAsync();
        await this.mapComponent.LoadRandomSystemAsync();
        await this.SaveGameAsync();
    }

    public async Task SaveGameAsync()
    {
        await this.saveSystem.SaveAsync(this.saveIndex);
    }

    public async Task<SaveSystem.SaveGameMetadata> LoadMetadataAsync(int index) => await this.saveSystem.LoadMetadataAsync(index);

    public async Task LoadGameAsync(int index)
    {
        this.saveIndex = index;
        if(await this.saveSystem.LoadAsync(this.saveIndex))
        {
            this.uiManager.ShowPlayUI();
        }
        else
        {
            await this.NewGameAsync();
        }
    }

    public async Task DeleteSaveAsync(int index)
    {
        if (await this.uiManager.ShowDialogAsync($"Delete save {index}?", DialogUI.Buttons.OkayCancel) == DialogUI.Buttons.Okay)
        {
            await SaveSystem.DeleteAsync(index);
        }
    }

    public void OpenMap() => this.uiManager.ShowMapUI();

    public void CloseMap() => this.uiManager.ShowPlayUI();

    public void OpenUpgrades() => this.uiManager.ShowUpgradeUI();

    public void CloseUpgrades() => this.uiManager.ShowPlayUI();

    public void OpenStarSystemUI() => this.uiManager.ShowStarSystemUI();

    public void CloseStarSystemUI() => this.uiManager.ShowPlayUI();

    public bool CanJump() => this.mapComponent.CanJump();

    public async void JumpAsync()
    {
        await this.mapComponent.JumpAsyc();
        await this.SaveGameAsync();
    }

    public void RestartLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    public void LoseGame()
    {
        this.uiManager.ShowLoseUI();

        this.player.gameObject.SetActive(false);
    }

}