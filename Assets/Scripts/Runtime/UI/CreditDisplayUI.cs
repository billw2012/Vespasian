// unset

using System;
using TMPro;
using UnityEngine;

public class CreditDisplayUI : MonoBehaviour
{
    public TMP_Text creditLabel;
    
    private Missions missions;

    private void Awake() => this.missions = FindObjectOfType<Missions>();

    private void Update() => this.creditLabel.text = $"{this.missions.playerCredits} cr";
}
