using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLogic : MonoBehaviour {
    public static GameLogic Instance;

    public GameObject player { get; private set; }

    public float collectedMinerals = 0;
    public float collectedGas = 0;
    public float health = 1;
    public float remainingFuel = 1;
    public float previousHealth = 1;
    public Vector3 lastDamageDirection;

    public float simTime;

    void Start()
    {
        this.player = GameObject.Find("Player");
        Instance = this;
        this.simTime = 0;
    }

    void Update()
    {
        if (this.player != null)
        {
            this.player.GetComponent<PlayerLogic>().SetTakingDamage((this.previousHealth - this.health) / Time.deltaTime, this.lastDamageDirection);
        }
        this.previousHealth = this.health;
    }

    void FixedUpdate()
    {
        this.simTime += Time.fixedDeltaTime;
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

    public void AddDamage(float amount, Vector3 direction)
    {
        this.health = Mathf.Clamp(this.health - amount, 0, 1);
        if(amount > 0)
        {
            this.lastDamageDirection = direction;
        }

        if(this.health == 0)
        {
            LoseGame();
        }
    }
}