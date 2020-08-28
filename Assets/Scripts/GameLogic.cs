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
        //this.playUI = GameObject.Find("PlayUI");
        //this.winUI = GameObject.Find("WinUI");
        //this.loseUI = GameObject.Find("LoseUI");

        //this.playUI.SetActive(true);
        //this.winUI.SetActive(false);
        //this.loseUI.SetActive(false);
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
        //this.playUI.SetActive(false);
        //this.winUI.SetActive(true);
    }

    public void LoseGame()
    {
       // this.playUI.SetActive(false);
        //this.loseUI.SetActive(true);
    }
}