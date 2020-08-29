

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

public class DragToFire : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameConstants constants;

    [Tooltip("How much force to apply")]
    public float forceCoefficient = 1.0f;

    PlayerLogic playerLogic;
    SimMovement playerMovement;

    Vector2 dragStart;
    Vector2 dragCurrent;

    void Start()
    {
        Assert.IsNotNull(this.constants);
        this.playerLogic = FindObjectOfType<PlayerLogic>();
        this.playerMovement = FindObjectOfType<SimMovement>();

        this.playerLogic.enabled = false;
        this.playerMovement.enabled = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        this.dragStart = eventData.position;
    }

    Vector3 GetVelocity()
    {
        // We always recalculate this as the camera might move
        var vec = (this.dragCurrent - this.dragStart) / Screen.width;
        return Vector3.ClampMagnitude(vec, 0.25f) * this.constants.GravitationalConstant * this.forceCoefficient /*/ Camera.main.orthographicSize*/;
    }

    public void OnDrag(PointerEventData eventData)
    {
        this.dragCurrent = eventData.position;
    }

    void Update()
    {
        this.playerMovement.velocity = this.GetVelocity();
        this.playerMovement.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.playerMovement.velocity);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        this.playerMovement.enabled = true;
        this.playerMovement.velocity = this.GetVelocity();
        this.playerLogic.enabled = true;
        this.enabled = false;
    }
}
