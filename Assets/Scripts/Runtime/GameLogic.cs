using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu]
public class GameLogic : ScriptableObject {
    private GUILayerManager uiManager;

    private MapComponent mapComponent;
    private PlayerController player;
    private SaveSystem saveSystem;

    private int saveIndex;
    private readonly SemaphoreSlim loadingSemaphore = new SemaphoreSlim(1, 1);

    private void OnEnable() => SceneManager.sceneLoaded += this.SceneManager_sceneLoaded;

    private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        this.uiManager = FindObjectOfType<GUILayerManager>();
        this.mapComponent = FindObjectOfType<MapComponent>();
        this.player = FindObjectOfType<PlayerController>();
        this.saveSystem = FindObjectOfType<SaveSystem>();
    }

    private async Task NewGameAsync()
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

        // Scoped lock to block other state changes while we load / start a new game
        this.loadingSemaphore.Wait();
        try
        {
            this.uiManager.ClearUI();
            if (!await this.saveSystem.LoadAsync(this.saveIndex))
            {
                await this.NewGameAsync();
            }

            this.uiManager.SwitchToPlay();
        }
        finally
        {
            this.loadingSemaphore.Release();
        }
    }

    public async Task DeleteSaveAsync(int index)
    {
        if (await this.uiManager.ShowDialogAsync($"Delete save {index}?", DialogUI.Buttons.OkayCancel) == DialogUI.Buttons.Okay)
        {
            await SaveSystem.DeleteAsync(index);
        }
    }

    public bool CanJump() => this.mapComponent.CanJump();

    public async void JumpAsync()
    {
        await this.mapComponent.JumpAsyc();
        await this.SaveGameAsync();
    }

    public void RestartLevel() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    public async void LoseGameAsync()
    {
        // Make sure we don't trigger lose screen while we are in some other transitional state
        await this.loadingSemaphore.WaitAsync();
        try
        {
            this.uiManager.SwitchToLose();

            this.player.gameObject.SetActive(false);
        }
        finally
        {
            this.loadingSemaphore.Release();
        }
    }

}