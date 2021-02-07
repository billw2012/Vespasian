using System.Linq;
using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    public GameLogic gameLogic;
    public GameObject nextButton;

    // Start is called before the first frame update
    private void Start()
    {
        this.nextButton.SetActive(false);
    }

    // Update is called once per frame
    private void Update()
    {
        if(!this.nextButton.activeSelf && ComponentCache.FindObjectsOfType<Objective>().Where(o => o.required).All(o => o.complete))
        {
            this.nextButton.SetActive(true);
        }
    }
}
