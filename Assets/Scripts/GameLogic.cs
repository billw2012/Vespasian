using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLogic : MonoBehaviour {
    public void RestartGame()
    {
        SceneManager.LoadScene("TestScene");
    }
}