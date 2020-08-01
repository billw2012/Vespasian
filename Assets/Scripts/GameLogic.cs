using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLogic : MonoBehaviour {
    public static GameLogic Instance;

    public GameObject player { get; private set; }

    public float collectedMinerals = 0;
    public float collectedGas = 0;
    public float health = 1;
    public float remainingFuel = 1;

    void Start()
    {
        this.player = GameObject.Find("Player");
        Instance = this;
    }

    public void RestartGame()
    {
        SceneManager.LoadScene("TestScene");
    }

    public void WinGame()
    {
        SceneManager.LoadScene("WinScene");
    }
}