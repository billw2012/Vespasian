// unset

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;

public class UIAttachmentPointManager : MonoBehaviour
{
    [SerializeField]
    private GameObject buttonPrefab;
    
    public enum UIAttachmentPoint
    {
        ContextActions,
        Weapon1,
        Weapon2,
    }

    [Serializable]
    public class UIAttachmentPointDef
    {
        public UIAttachmentPoint id;
        public RectTransform root;
    }
    
    [SerializeField] private List<UIAttachmentPointDef> attachmentPoints = null;

    private RectTransform GetAttachmentPoint(UIAttachmentPoint id) => this.attachmentPoints.First(a => a.id == id).root;
    
    public void Attach(UIAttachmentPoint where, RectTransform ui) => ui.SetParent(this.GetAttachmentPoint(where));
    
    public GameObject AttachButton(UIAttachmentPoint where, string label, UnityAction onClick, bool active = false)
    {
        var button = Instantiate(this.buttonPrefab, this.GetAttachmentPoint(where));
        button.SetActive(active);
        button.GetComponentInChildren<Button>().onClick.AddListener(onClick);
        button.GetComponentInChildren<TMP_Text>().text = label;
        return button;
    }    
    
    public GameObject AttachButton(UIAttachmentPoint where, string label, Action<EventTrigger> bindActions, bool active = false)
    {
        var button = Instantiate(this.buttonPrefab, this.GetAttachmentPoint(where));
        button.SetActive(active);
        //button.GetComponentInChildren<Button>().onClick.AddListener(onClick);
        bindActions(button.GetComponentInChildren<EventTrigger>());
        button.GetComponentInChildren<TMP_Text>().text = label;
        return button;
    }

    //[SerializeField] private RectTransform contextActionUIRoot;
    //[SerializeField] private RectTransform weaponUIRoot;

    /// <summary>
    /// Adds a context action from a prefab
    /// </summary>
    /// <param name="prefab"></param>
    /// <returns></returns>
    //public GameObject AddContextAction(GameObject prefab) => Instantiate(prefab, this.contextActionUIRoot);
    /// <summary>
    /// Adds a context action from a prefab, also connecting the onClick handler of its button.
    /// This assumes the prefab contains one Button.
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="onClick"></param>
    /// <returns></returns>
    // public GameObject AddContextAction(GameObject prefab, UnityAction onClick)
    // {
    //     var obj = this.AddContextAction(prefab);
    //     var button = obj.GetComponentInChildren<Button>();
    //     button.onClick.AddListener(onClick);
    //     return obj;
    // }

    /// <summary>
    /// Adds a context action from a prefab, also connecting press and release handlers.
    /// This assumes the prefab contains one EventTrigger.
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="onPress"></param>
    /// <param name="onRelease"></param>
    /// <returns></returns>
    // public GameObject AddContextAction(GameObject prefab, UnityAction<PointerEventData> onPress, UnityAction<PointerEventData> onRelease)
    // {
    //     var obj = this.AddContextAction(prefab);
    //     var eventTrigger = obj.GetComponentInChildren<EventTrigger>();
    //
    //     var pointerDown = new EventTrigger.Entry {eventID = EventTriggerType.PointerDown};
    //     pointerDown.callback.AddListener(data => onPress.Invoke((PointerEventData) data));
    //     eventTrigger.triggers.Add(pointerDown);
    //     var pointerUp = new EventTrigger.Entry {eventID = EventTriggerType.PointerUp};
    //     pointerUp.callback.AddListener(data => onRelease.Invoke((PointerEventData) data));
    //     eventTrigger.triggers.Add(pointerUp);
    //
    //     return obj;
    // }
    //
    // //public void AddWeaponUI(GameObject ui) => ui.transform.SetParent(this.contextActionUIRoot);
    //
    // public void Remove(GameObject ui)
    // {
    //     ui.SetActive(false);
    //     ui.transform.SetParent(null);
    //     Destroy(ui);
    // }

}