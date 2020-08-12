

using UnityEngine;
using UnityEngine.EventSystems;

public class DragToFire : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {

    public GameObject ObjectToFire = null;
    public float ForceCoefficient = 1.0f;

    Vector2 dragStart;

    PlayerLogic playerLogic => this.ObjectToFire.GetComponent<PlayerLogic>();
    bool launched => this.playerLogic.enabled;

    void Start()
    {
        Debug.Assert(this.ObjectToFire != null);
        // Freeze the object in place until we impart the velocity we want to
        this.ObjectToFire.GetComponent<PlayerLogic>().enabled = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Assert(this.ObjectToFire != null);
        this.dragStart = eventData.position;
    }

    private Vector3 GetVelocity(Vector3 dragPosition)
    {
        // We always recalculate this as the camera might move
        var startPosition = Camera.main.ScreenToWorldPoint(this.dragStart);
        var position = Camera.main.ScreenToWorldPoint(dragPosition);
        return Vector3.ClampMagnitude(position - startPosition, GameConstants.Instance.MaxLaunchVelocity) * GameConstants.Instance.GlobalCoefficient * this.ForceCoefficient;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Assert(this.ObjectToFire != null);
        
        this.playerLogic.velocity = this.GetVelocity(eventData.position);

        this.ObjectToFire.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.playerLogic.velocity);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Assert(this.ObjectToFire != null);
        this.playerLogic.enabled = true;
        this.playerLogic.velocity = this.GetVelocity(eventData.position);
    }
}
