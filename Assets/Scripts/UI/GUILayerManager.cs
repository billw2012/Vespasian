using Pixelplacement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUILayerManager : MonoBehaviour
{
    public DisplayObject playUI;
    public DisplayObject winUI;
    public DisplayObject loseUI;

    void Start()
    {
        this.playUI.SetActive(true);
        this.winUI.SetActive(false);
        this.loseUI.SetActive(false);
    }

    public void ShowWinUI()
    {
        this.playUI.SetActive(false);
        this.winUI.SetActive(true);
        this.loseUI.SetActive(false);
    }

    public void ShowLoseUI()
    {
        this.playUI.SetActive(false);
        this.winUI.SetActive(false);
        this.loseUI.SetActive(true);
    }
}
