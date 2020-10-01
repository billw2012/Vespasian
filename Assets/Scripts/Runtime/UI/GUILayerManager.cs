using Pixelplacement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUILayerManager : MonoBehaviour
{
    public DisplayObject playUI;
    public DisplayObject loseUI;
    public DisplayObject mapUI;

    DisplayObject[] all;

    void Awake()
    {
        this.all = new[] { this.playUI, this.loseUI, this.mapUI };
    }

    void Start()
    {
        this.ShowPlayUI();
    }

    public void ShowMapUI()
    {
        this.Enable(this.mapUI);
    }

    public void HideUI()
    {
        this.Enable(null);
    }

    public void ShowPlayUI()
    {
        this.Enable(this.playUI);
    }

    public void ShowLoseUI()
    {
        this.Enable(this.loseUI);
    }

    void Enable(DisplayObject uiToEnable)
    {
        foreach(var ui in this.all)
        {
            ui.SetActive(ui == uiToEnable);
        }
    }
}
