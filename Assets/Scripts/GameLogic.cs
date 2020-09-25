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

    public void WinGame()
    {
        this.uiManager.ShowWinUI();

        GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>().text = $"{this.CalculateScore()} points";

        this.player.gameObject.SetActive(false);
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

    int CalculateScore()
    {
        int score = 0;

        // 1 point for each % of fuel remaining
        score += Mathf.FloorToInt(this.player.GetComponent<EngineComponent>().fuelCurrent * 100f);

        // 1 point for each % of health remaining
        score += Mathf.FloorToInt(this.player.GetComponent<HealthComponent>().hull * 100f);

        // 0.2 point for each % of planets scanned
        score += Mathf.FloorToInt(FindObjectsOfType<Scannable>().Select(e => e.scanProgress).Sum() * 20f);

        return score;
    }
}