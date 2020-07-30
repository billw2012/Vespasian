

using UnityEngine;
using UnityEngine.EventSystems;

public class DragToFire : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {

    public GameObject ObjectToFire = null;
    public float ForceCoefficient = 1.0f;

    GameObject lineObject;

    Vector3 dragStart;

    void Start()
    {
        Debug.Assert(this.ObjectToFire != null);
        // Freeze the object in place until we impart the velocity we want to
        this.ObjectToFire.GetComponent<GravityAffected>().enabled = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Assert(this.ObjectToFire != null);
        this.dragStart = Camera.main.ScreenToWorldPoint(eventData.position);
        this.lineObject = new GameObject();
        var lineRenderer = this.lineObject.AddComponent<LineRenderer>();
        lineRenderer.SetPositions(new Vector3[]{ this.ObjectToFire.transform.position, this.ObjectToFire.transform.position });
        lineRenderer.startWidth = lineRenderer.endWidth = 0.1f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Assert(this.ObjectToFire != null);
        var position = Camera.main.ScreenToWorldPoint(eventData.position);
        var lineRenderer = this.lineObject.GetComponent<LineRenderer>();
        var vector = position - this.dragStart;
        lineRenderer.SetPositions(new Vector3[]{ this.ObjectToFire.transform.position, this.ObjectToFire.transform.position + vector });

        this.ObjectToFire.transform.rotation = Quaternion.FromToRotation(Vector3.up, vector);

        var gravityAffected = this.ObjectToFire.GetComponent<GravityAffected>();
        gravityAffected.Simulate(GameConstants.Instance.GlobalCoefficient * vector * this.ForceCoefficient).ContinueWith(_ => { });
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Assert(this.ObjectToFire != null);
        var gravityAffected = this.ObjectToFire.GetComponent<GravityAffected>();
        gravityAffected.enabled = true;
        var position = Camera.main.ScreenToWorldPoint(eventData.position);
        var vector = position - this.dragStart;
        gravityAffected.velocity = GameConstants.Instance.GlobalCoefficient * vector * this.ForceCoefficient;
        gravityAffected.ClearSimulation();
        Destroy(this.lineObject);
        this.lineObject = null;
    }
}