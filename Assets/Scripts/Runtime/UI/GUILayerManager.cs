using Pixelplacement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Optional interface that a UILayer can implement to provide extended functionality
/// </summary>
interface IUILayer
{
    /// <summary>
    /// When first added to the stack
    /// </summary>
    void OnAdded();
    /// <summary>
    /// When removed from the stack
    /// </summary>
    void OnRemoved();
    /// <summary>
    /// When another layer is added on top of this one
    /// </summary>
    void OnDemoted();
    /// <summary>
    /// When layer on top of this one is removed, revealing this one again
    /// </summary>
    void OnPromoted();
}

/// <summary>
/// Provides functions for manipulating the default UI layers, and using custom ones, 
/// showing dialogs.
/// It allows a mixture of stack based UI and replacing the entire stack with a new layer.
/// </summary>
public class GUILayerManager : MonoBehaviour
{
    public GameObject startUI;
    public GameObject playUI;
    public GameObject loseUI;

    public GameObject dialogPrefab;

    private readonly List<GameObject> layerStack = new List<GameObject>();

    private void Start() => this.SwitchToStartUI();

    public void ClearUI() => this.SwitchTo(null);

    public void SwitchToStartUI() => this.SwitchTo(this.startUI);

    public void SwitchToPlay() => this.SwitchTo(this.playUI);

    public void SwitchToLose() => this.SwitchTo(this.loseUI);

    /// <summary>
    /// Show whatever the current layer is (probably previously hidden using HideUI)
    /// </summary>
    public void ShowUI() => SetActiveOptionalDisplayObject(this.layerStack.LastOrDefault(), true);

    /// <summary>
    /// Hide the current UI layer
    /// </summary>
    public void HideUI() => SetActiveOptionalDisplayObject(this.layerStack.LastOrDefault(), false);

    /// <summary>
    /// Replace layer stack with a single new layer, e.g. when transitioning
    /// from main menu to game play we don't want to keep the main menu stack.
    /// </summary>
    /// <param name="layer"></param>
    public void SwitchTo(GameObject layer)
    {
        SetActiveOptionalDisplayObject(this.layerStack.LastOrDefault(), false);
        foreach (var oldLayer in this.layerStack)
        {
            oldLayer.GetComponent<IUILayer>()?.OnRemoved();
        }
        this.layerStack.Clear();
        if (layer != null)
        {
            this.PushLayer(layer);
        }
    }

    /// <summary>
    /// Hide the current top layer, and add one on top of it
    /// </summary>
    /// <param name="layer">A custom layer</param>
    public void PushLayer(GameObject layer)
    {
        SetActiveOptionalDisplayObject(this.layerStack.LastOrDefault(), false);
        this.layerStack.LastOrDefault()?.GetComponent<IUILayer>()?.OnDemoted();
        this.layerStack.Add(layer);
        SetActiveOptionalDisplayObject(layer, true);
        layer.GetComponent<IUILayer>()?.OnAdded();
    }

    /// <summary>
    /// Hide and remove the current top layer, and show the one below it (if there is one)
    /// </summary>
    public void PopLayer()
    {
        SetActiveOptionalDisplayObject(this.layerStack.Last(), false);
        this.layerStack.Last().GetComponent<IUILayer>()?.OnRemoved();
        this.layerStack.RemoveAt(this.layerStack.Count - 1);
        SetActiveOptionalDisplayObject(this.layerStack.LastOrDefault(), true);
        this.layerStack.LastOrDefault()?.GetComponent<IUILayer>()?.OnPromoted();
    }

    /// <summary>
    /// Show a dialog with the specified text content and buttons
    /// </summary>
    /// <param name="content"></param>
    /// <param name="buttons"></param>
    /// <returns></returns>
    public async Task<DialogUI.Buttons> ShowDialogAsync(string content, DialogUI.Buttons buttons = DialogUI.Buttons.Okay)
    {
        var dialog = ComponentCache.Instantiate(this.dialogPrefab, this.transform);
        this.PushLayer(dialog);
        var result = await dialog.GetComponent<DialogUI>().Show(content, buttons);
        this.PopLayer();
        Destroy(dialog);
        return result;
    }

    private static void SetActiveOptionalDisplayObject(GameObject obj, bool active)
    {
        // Makes the rest of the code neater if obj can be null...
        if (obj == null)
        {
            return;
        }
        var displayObject = obj.GetComponent<DisplayObject>();
        if (displayObject != null)
        {
            displayObject.SetActive(active);
        }
        else
        {
            obj.SetActive(active);
        }
    }
}
