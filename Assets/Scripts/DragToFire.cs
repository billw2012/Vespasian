

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

public class DragToFire : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PlayerLogic objectToFire = null;

    [Tooltip("How much force to apply")]
    public float forceCoefficient = 1.0f;

    public GameConstants constants;

    Vector2 dragStart;

    void Start()
    {
        Assert.IsNotNull(this.objectToFire);
        Assert.IsNotNull(this.constants);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (this.objectToFire.state == PlayerLogic.FlyingState.Aiming)
        {
            this.dragStart = eventData.position;
        }
    }

    Vector3 GetVelocity(Vector3 dragPosition)
    {
        // We always recalculate this as the camera might move
        var startPosition = Camera.main.ScreenToWorldPoint(this.dragStart);
        var position = Camera.main.ScreenToWorldPoint(dragPosition);
        return Vector3.ClampMagnitude(position - startPosition, this.constants.MaxLaunchVelocity) * this.constants.GravitationalConstant * this.forceCoefficient * 2 / Camera.main.orthographicSize;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (this.objectToFire.state == PlayerLogic.FlyingState.Aiming)
        {
            this.objectToFire.velocity = this.GetVelocity(eventData.position);
            this.objectToFire.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.objectToFire.velocity);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (this.objectToFire.state == PlayerLogic.FlyingState.Aiming)
        {
            this.objectToFire.velocity = this.GetVelocity(eventData.position);
            this.objectToFire.state = PlayerLogic.FlyingState.Flying;
        }
    }
}
