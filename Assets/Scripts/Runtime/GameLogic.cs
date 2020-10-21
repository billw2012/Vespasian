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

    int saveIndex;

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

    public async Task SaveGameAsync()
    {
        var playerSimMovement = this.player.GetComponent<SimMovement>();
        var playerUpgrades = this.player.GetComponent<UpgradeManager>();
        var simManager = FindObjectOfType<SimManager>();
        var saveData = new SaveSystem.SaveGameData
        {
            map = this.mapComponent.map,
            system = this.mapComponent.currentSystem,
            playerPosition = playerSimMovement.transform.position,
            playerRotation = playerSimMovement.transform.rotation,
            playerVelocity = playerSimMovement.velocity,
            simTick = simManager.simTick,
            installedUpgrades = playerUpgrades.SaveUpgrades(),
        };

        await SaveSystem.SaveAsync(this.saveIndex, saveData);
    }

    public async Task<SaveSystem.SaveGameMetadata> LoadMetadataAsync(int index) => await SaveSystem.LoadMetadataAsync(index);

    public async Task LoadGameAsync(int index)
    {
        this.saveIndex = index;
        var saveData = await SaveSystem.LoadAsync(this.saveIndex);
        if(saveData != null)
        {
            var playerSimMovement = this.player.GetComponent<SimMovement>();
            var playerUpgrades = this.player.GetComponent<UpgradeManager>();
            var simManager = FindObjectOfType<SimManager>();

            this.mapComponent.map = saveData.map;
            simManager.simTick = saveData.simTick;
            playerUpgrades.LoadUpgrades(saveData.installedUpgrades);
            playerSimMovement.SetPositionVelocity(saveData.playerPosition, saveData.playerRotation, saveData.playerVelocity);
            await this.mapComponent.LoadSystemAsync(saveData.system);
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