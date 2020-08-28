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
    }

    public void LoseGame()
    {
        this.playUI.SetActive(false);
        this.loseUI.SetActive(true);
    }
}