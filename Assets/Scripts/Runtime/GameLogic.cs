using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
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

    // Events
    public UnityEvent OnNewGameInitialized = new UnityEvent();

    private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        this.uiManager = ComponentCache.FindObjectOfType<GUILayerManager>();
        this.mapComponent = ComponentCache.FindObjectOfType<MapComponent>();
        this.player = ComponentCache.FindObjectOfType<PlayerController>();
        this.saveSystem = ComponentCache.FindObjectOfType<SaveSystem>();
    }

    private async Task NewGameAsync()
    {
        await this.mapComponent.GenerateMapAsync();
        await this.mapComponent.LoadStartingSystemAsync();
        await this.SaveGameAsync();
        this.OnNewGameInitialized.Invoke();
    }

    public async Task SaveGameAsync()
    {
        await this.saveSystem.SaveAsync(this.saveIndex);
        NotificationsUI.Add($"<style=system>Game saved in slot {this.saveIndex + 1}");
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
                NotificationsUI.Add($"<style=system>New game started");
            }
            else
            {
                NotificationsUI.Add($"<style=system>Game loaded from slot {this.saveIndex + 1}");
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