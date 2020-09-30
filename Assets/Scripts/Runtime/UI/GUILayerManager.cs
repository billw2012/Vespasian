using Pixelplacement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUILayerManager : MonoBehaviour
{
    public DisplayObject playUI;
    public DisplayObject winUI;
    public DisplayObject loseUI;
    public DisplayObject mapUI;

    // TODO: 4 layers is about the limit to manage manually, should probably setup some rules
    // instead. If they are all mutually exclusive then that is quite easy...
    // Maybe an enum and collection?
    void Start()
    {
        this.playUI.SetActive(true);
        this.winUI.SetActive(false);
        this.mapUI.SetActive(false);
        this.loseUI.SetActive(false);
    }

    public void ShowMapUI()
    {
        this.playUI.SetActive(false);
        this.winUI.SetActive(false);
        this.mapUI.SetActive(true);
        this.loseUI.SetActive(false);
    }

    public void ShowPlayUI()
    {
        this.playUI.SetActive(true);
        this.winUI.SetActive(false);
        this.mapUI.SetActive(false);
        this.loseUI.SetActive(false);
    }
    public void ShowWinUI()
    {
        this.winUI.SetActive(true);
        this.playUI.SetActive(false);
        this.mapUI.SetActive(false);
        this.loseUI.SetActive(false);
    }

    public void ShowLoseUI()
    {
        this.winUI.SetActive(false);
        this.playUI.SetActive(false);
        this.mapUI.SetActive(false);
        this.loseUI.SetActive(true);
    }
}
