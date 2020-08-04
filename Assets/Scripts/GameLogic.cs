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

    public void LoseGame()
    {
        SceneManager.LoadScene("LoseScene");
    }

    public void AddFuel(float amount)
    {
        this.remainingFuel = Mathf.Clamp(this.remainingFuel + amount, 0, 1.25f);
    }

    public void AddDamage(float amount)
    {
        this.health = Mathf.Clamp(this.health - amount, 0, 1);
        if(this.health == 0)
        {
            LoseGame();
        }
    }
}