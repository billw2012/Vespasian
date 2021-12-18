// unset

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ContextActionManager : MonoBehaviour
{
    public delegate bool ActivePredicate();

    private class ContextAction
    {
        public string name;
        public ActivePredicate predicate;
        public GameObject uiObject;
    }

    private List<ContextAction> actions = new List<ContextAction>();
    private UIAttachmentPointManager uiAttachmentPointManager;

    private void Awake()
    {
        this.uiAttachmentPointManager = ComponentCache.FindObjectOfType<UIAttachmentPointManager>();
    }
    
    public void Add(string action, UnityAction onActivated, ActivePredicate activePredicate) =>
        this.actions.Add(new ContextAction {
            name = action,
            predicate = activePredicate,
            uiObject = this.uiAttachmentPointManager.AttachButton(
                UIAttachmentPointManager.UIAttachmentPoint.ContextActions,
                action,
                onActivated,
                activePredicate()),
        });

    public void Add(string action, Action<EventTrigger> bindActions, ActivePredicate activePredicate) =>
        this.actions.Add(new ContextAction {
            name = action,
            predicate = activePredicate,
            uiObject = this.uiAttachmentPointManager.AttachButton(
                UIAttachmentPointManager.UIAttachmentPoint.ContextActions,
                action,
                bindActions,
                activePredicate()),
        });
    
    public void Add(string action, UnityAction onDown, UnityAction onUp, ActivePredicate activePredicate) =>
        this.actions.Add(new ContextAction {
            name = action,
            predicate = activePredicate,
            uiObject = this.uiAttachmentPointManager.AttachButton(
                UIAttachmentPointManager.UIAttachmentPoint.ContextActions,
                action,
                eventTrigger => eventTrigger
                    .Add(EventTriggerType.PointerDown, onDown)
                    .Add(EventTriggerType.PointerUp, onUp),
                activePredicate()),
        });

    public bool Remove(string action)
    {
        var contextAction = this.actions.FirstOrDefault(a => a.name == action);
        if (contextAction != null)
        {
            contextAction.uiObject.SetActive(false);
            Destroy(contextAction.uiObject);
            this.actions.Remove(contextAction);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Update()
    {
        // Every tick? Leave it to callers to make sure their predicates are optimized...
        foreach (var action in this.actions)
        {
            action.uiObject.SetActive(action.predicate());
        }
    }
}