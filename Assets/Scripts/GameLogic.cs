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

    void OnEnable()
    {
        SceneManager.sceneLoaded += this.SceneManager_sceneLoaded;
    }

    void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        this.uiManager = FindObjectOfType<GUILayerManager>();
    }

    public void NextLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void WinGame()
    {
        this.uiManager.ShowWinUI();

        GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>().text = $"{this.CalculateScore()} points";

        FindObjectOfType<PlayerLogic>().gameObject.SetActive(false);
    }

    public void LoseGame()
    {
        this.uiManager.ShowLoseUI();

        FindObjectOfType<PlayerLogic>().gameObject.SetActive(false);
    }

    int CalculateScore()
    {
        int score = 0;

        var player = FindObjectOfType<PlayerLogic>().gameObject;

        // 1 point for each % of fuel remaining
        score += Mathf.FloorToInt(player.GetComponent<PlayerLogic>().fuelCurrent * 100f);

        // 1 point for each % of health remaining
        score += Mathf.FloorToInt(player.GetComponent<HealthComponent>().hull * 100f);

        // 0.2 point for each % of planets scanned
        score += Mathf.FloorToInt(FindObjectsOfType<Scannable>().Select(e => e.scanProgress).Sum() * 20f);

        return score;
    }
}