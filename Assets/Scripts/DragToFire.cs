

using UnityEngine;
using UnityEngine.EventSystems;

public class DragToFire : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {

    public GameObject ObjectToFire = null;
    public float ForceCoefficient = 1.0f;

    Vector2 dragStart;

    PlayerLogic playerLogic => this.ObjectToFire.GetComponent<PlayerLogic>();

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Assert(this.ObjectToFire != null);

        if (this.playerLogic.state == PlayerLogic.FlyingState.Aiming)
        {
            this.dragStart = eventData.position;
        }
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

        if (this.playerLogic.state == PlayerLogic.FlyingState.Aiming)
        {
            this.playerLogic.velocity = this.GetVelocity(eventData.position);

            this.ObjectToFire.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.playerLogic.velocity);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Assert(this.ObjectToFire != null);

        if (this.playerLogic.state == PlayerLogic.FlyingState.Aiming)
        {
            this.playerLogic.velocity = this.GetVelocity(eventData.position);

            this.playerLogic.state = PlayerLogic.FlyingState.Flying;
        }
    }
}
