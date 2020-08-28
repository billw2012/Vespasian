

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

public class DragToFire : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameConstants constants;

    [Tooltip("How much force to apply")]
    public float forceCoefficient = 1.0f;

    PlayerLogic player;

    Vector2 dragStart;
    Vector2 dragCurrent;

    void Start()
    {
        Assert.IsNotNull(this.constants);
        this.player = FindObjectOfType<PlayerLogic>();
        Assert.IsNotNull(this.player);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (this.player.state == PlayerLogic.FlyingState.Aiming)
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
        if (this.player.state == PlayerLogic.FlyingState.Aiming)
        {
            this.dragCurrent = eventData.position;
        }
    }

    void Update()
    {
        if (this.player.state == PlayerLogic.FlyingState.Aiming)
        {
            this.player.velocity = this.GetVelocity();
            this.player.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.player.velocity);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (this.player.state == PlayerLogic.FlyingState.Aiming)
        {
            this.player.velocity = this.GetVelocity();
            this.player.state = PlayerLogic.FlyingState.Flying;
        }
    }
}
