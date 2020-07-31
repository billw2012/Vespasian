using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLogic : MonoBehaviour {
    public static GameLogic Instance;

    public GameObject player;

    void Start()
    {
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