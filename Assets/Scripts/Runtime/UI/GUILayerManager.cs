using Pixelplacement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class GUILayerManager : MonoBehaviour
{
    public DisplayObject mainMenuUI;
    public DisplayObject playUI;
    public DisplayObject loseUI;
    public DisplayObject mapUI;
    public DisplayObject upgradeUI;

    public GameObject dialogPrefab;

    DisplayObject[] all;

    public DisplayObject activeUI => this.all.FirstOrDefault(ui => ui.isActiveAndEnabled);

    void Awake() => this.all = new[] { this.playUI, this.loseUI, this.mapUI, this.mainMenuUI, this.upgradeUI };

    // void Start() => this.ShowMainMenuUI();

    public void ShowMainMenuUI() => this.Enable(this.mainMenuUI);
    public void ShowPlayUI() => this.Enable(this.playUI);
    public void ShowMapUI() => this.Enable(this.mapUI);
    public void ShowUpgradeUI() => this.Enable(this.upgradeUI);
    public void ShowLoseUI() => this.Enable(this.loseUI);
    public void HideUI() => this.Enable(null);

    void Enable(DisplayObject uiToEnable)
    {
        foreach(var ui in this.all)
        {
            ui.SetActive(ui == uiToEnable);
        }
    }

    public async Task<DialogUI.Buttons> ShowDialogAsync(string content, DialogUI.Buttons buttons = DialogUI.Buttons.Okay)
    {
        var prevUI = this.activeUI;
        this.HideUI();
        var dialog = Instantiate(this.dialogPrefab, this.transform);
        dialog.SetActive(true);
        var result = await dialog.GetComponent<DialogUI>().Show(content, buttons);
        dialog.SetActive(false);
        Destroy(dialog);
        this.Enable(prevUI);
        return result;
    }
}
