

using UnityEngine;
using UnityEngine.EventSystems;

public class DragToFire : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {

    public GameObject ObjectToFire = null;
    public float ForceCoefficient = 1.0f;

    GameObject lineObject;

    Vector2 dragStart;

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
        this.lineObject = new GameObject();
        var lineRenderer = this.lineObject.AddComponent<LineRenderer>();
        lineRenderer.SetPositions(new Vector3[]{ this.ObjectToFire.transform.position, this.ObjectToFire.transform.position });
        lineRenderer.startWidth = lineRenderer.endWidth = 0.1f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Assert(this.ObjectToFire != null);

        // We always recalculate this as the camera might move
        var startPosition = Camera.main.ScreenToWorldPoint(this.dragStart);
        var position = Camera.main.ScreenToWorldPoint(eventData.position);
        var lineRenderer = this.lineObject.GetComponent<LineRenderer>();
        var vector = position - startPosition;
        lineRenderer.SetPositions(new Vector3[]{ this.ObjectToFire.transform.position, this.ObjectToFire.transform.position + vector });

        this.ObjectToFire.transform.rotation = Quaternion.FromToRotation(Vector3.up, vector);

        var playerLogic = this.ObjectToFire.GetComponent<PlayerLogic>();
        playerLogic.velocity = GameConstants.Instance.GlobalCoefficient * vector * this.ForceCoefficient;
        playerLogic.Simulate(playerLogic.velocity).ContinueWith(_ => { });
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Assert(this.ObjectToFire != null);
        var playerLogic = this.ObjectToFire.GetComponent<PlayerLogic>();
        playerLogic.enabled = true;
        var startPosition = Camera.main.ScreenToWorldPoint(this.dragStart);
        var position = Camera.main.ScreenToWorldPoint(eventData.position);
        var vector = position - startPosition;
        playerLogic.velocity = GameConstants.Instance.GlobalCoefficient * vector * this.ForceCoefficient;
        playerLogic.ClearSimulation();
        Destroy(this.lineObject);
        this.lineObject = null;
    }
}