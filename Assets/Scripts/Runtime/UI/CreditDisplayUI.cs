// unset

using TMPro;
using UnityEngine;

public class CreditDisplayUI : MonoBehaviour
{
    public TMP_Text creditLabel;
    
    private Missions missions;

    private void Awake() => this.missions = ComponentCache.FindObjectOfType<Missions>();

    private void Update() => this.creditLabel.text = $"<style=credits>{this.missions.playerCredits} cr</style>";
}
