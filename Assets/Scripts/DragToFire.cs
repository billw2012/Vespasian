

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
    Vector2 dragCurrent;

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

    Vector3 GetVelocity()
    {
        // We always recalculate this as the camera might move
        var vec = (this.dragCurrent - this.dragStart) / Screen.width;
        return Vector3.ClampMagnitude(vec, 0.25f) * this.constants.GravitationalConstant * this.forceCoefficient /*/ Camera.main.orthographicSize*/;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (this.objectToFire.state == PlayerLogic.FlyingState.Aiming)
        {
            this.dragCurrent = eventData.position;
        }
    }

    void Update()
    {
        if (this.objectToFire.state == PlayerLogic.FlyingState.Aiming)
        {
            this.objectToFire.velocity = this.GetVelocity();
            this.objectToFire.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.objectToFire.velocity);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (this.objectToFire.state == PlayerLogic.FlyingState.Aiming)
        {
            this.objectToFire.velocity = this.GetVelocity();
            this.objectToFire.state = PlayerLogic.FlyingState.Flying;
        }
    }
}
