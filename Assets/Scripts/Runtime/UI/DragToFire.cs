

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

public class DragToFire : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameConstants constants;

    [Tooltip("How much force to apply")]
    public float forceCoefficient = 1.0f;

    private PlayerController playerLogic;
    private SimMovement playerMovement;

    private Vector2 dragStart;
    private Vector2 dragCurrent;

    private void Start()
    {
        Assert.IsNotNull(this.constants);
        this.playerLogic = ComponentCache.FindObjectOfType<PlayerController>();
        this.playerMovement = ComponentCache.FindObjectOfType<SimMovement>();

        if (this.playerMovement.startVelocity.magnitude > 0)
        {
            Debug.Log("Disabling DragToFire as player simMovement startVelocity is not zero");
            this.enabled = false;
        }
        else
        {
            this.playerLogic.enabled = false;
            this.playerMovement.enabled = false;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        this.dragStart = eventData.position;
    }

    private Vector3 GetVelocity()
    {
        // We always recalculate this as the camera might move
        var vec = (this.dragCurrent - this.dragStart) / Screen.width;
        return Vector3.ClampMagnitude(vec, 0.25f) * this.constants.GravitationalConstant * this.forceCoefficient /*/ GUILayerManager.MainCamera.orthographicSize*/;
    }

    public void OnDrag(PointerEventData eventData)
    {
        this.dragCurrent = eventData.position;
    }

    private void Update()
    {
        this.playerMovement.startVelocity = this.GetVelocity();
        this.playerMovement.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.playerMovement.startVelocity);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        this.playerMovement.enabled = true;
        this.playerMovement.startVelocity = this.GetVelocity();
        this.playerLogic.enabled = true;
        this.enabled = false;
    }
}
