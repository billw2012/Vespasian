using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu]
public class GameLogic : ScriptableObject {
    [HideInInspector]
    public float collectedMinerals = 0;
    [HideInInspector]
    public float collectedGas = 0;

    GUILayerManager uiManager;
    MapComponent mapComponent;
    PlayerController player;

    void OnEnable()
    {
        SceneManager.sceneLoaded += this.SceneManager_sceneLoaded;
    }

    void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        this.uiManager = FindObjectOfType<GUILayerManager>();
        this.mapComponent = FindObjectOfType<MapComponent>();
        this.player = FindObjectOfType<PlayerController>();
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