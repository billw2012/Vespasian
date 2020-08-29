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

    GameObject playUI;
    GameObject winUI;
    GameObject loseUI;

    void OnEnable()
    {
        SceneManager.sceneLoaded += this.SceneManager_sceneLoaded;
    }

    void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
    {
        this.playUI = GameObject.Find("Play UI");
        this.winUI = GameObject.Find("Win UI");
        this.loseUI = GameObject.Find("Lose UI");

        this.winUI.SetActive(false);
        this.loseUI.SetActive(false);
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
        this.playUI.SetActive(false);
        this.winUI.SetActive(true);
        GameObject.Find("ScoreText").GetComponent<TextMeshProUGUI>().text = $"{this.CalculateScore()} points";

        FindObjectOfType<PlayerLogic>().gameObject.SetActive(false);
    }

    public void LoseGame()
    {
        this.playUI.SetActive(false);
        this.loseUI.SetActive(true);
        FindObjectOfType<PlayerLogic>().gameObject.SetActive(false);
    }

    int CalculateScore()
    {
        int score = 0;

        var player = FindObjectOfType<PlayerLogic>().gameObject;

        // 1 point for each % of fuel remaining
        score += Mathf.FloorToInt(player.GetComponent<PlayerLogic>().fuelCurrent * 100f);

        // 1 point for each % of health remaining
        score += Mathf.FloorToInt(player.GetComponent<HealthComponent>().health * 100f);

        // 0.2 point for each % of planets scanned
        score += Mathf.FloorToInt(FindObjectsOfType<ScanEffect>().Select(e => e.scanned).Sum() * 20f);

        return score;
    }
}