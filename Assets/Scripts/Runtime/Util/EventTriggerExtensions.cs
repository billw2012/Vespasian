// unset

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public static class EventTriggerExtensions
{
    public static EventTrigger Add(this EventTrigger @this, EventTriggerType trigger, UnityAction action)
    {
        var pointerDown = new EventTrigger.Entry { eventID = trigger };
        pointerDown.callback.AddListener(_ => action());
        @this.triggers.Add(pointerDown);
        return @this;
    }
    
    public static EventTrigger Add<T>(this EventTrigger @this, EventTriggerType trigger, UnityAction<T> action) 
        where T : BaseEventData
    {
        var pointerDown = new EventTrigger.Entry { eventID = trigger };
        pointerDown.callback.AddListener(data => action(data as T));
        @this.triggers.Add(pointerDown);
        return @this;
    }
}
