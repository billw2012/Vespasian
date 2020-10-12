using Pixelplacement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUILayerManager : MonoBehaviour
{
    public DisplayObject mainMenuUI;
    public DisplayObject playUI;
    public DisplayObject loseUI;
    public DisplayObject mapUI;

    DisplayObject[] all;

    void Awake() => this.all = new[] { this.playUI, this.loseUI, this.mapUI, this.mainMenuUI };

    // void Start() => this.ShowMainMenuUI();

    public void ShowMainMenuUI() => this.Enable(this.mainMenuUI);
    public void ShowPlayUI() => this.Enable(this.playUI);
    public void ShowMapUI() => this.Enable(this.mapUI);
    public void ShowLoseUI() => this.Enable(this.loseUI);
    public void HideUI() => this.Enable(null);

    void Enable(DisplayObject uiToEnable)
    {
        foreach(var ui in this.all)
        {
            ui.SetActive(ui == uiToEnable);
        }
    }

}
