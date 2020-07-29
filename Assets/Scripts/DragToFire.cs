

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

    // void OnMouseDown()
    // {
    // }

    // void OnMouseDrag()
    // {
    //     var lineRenderer = this.lineObject.GetComponent<LineRenderer>();
    //     lineRenderer.SetPositions(new Vector3[]{ lineRenderer.GetPosition(0), Input.mousePosition });
    // }

    // void OnMouseUp()
    // {
    //     if(this.ObjectToFire != null)
    //     {
    //         var objectRigidBody2D = this.ObjectToFire.GetComponent<Rigidbody2D>();
    //         objectRigidBody2D.isKinematic = false;
    //         var lineRenderer = this.lineObject.GetComponent<LineRenderer>();
    //         var force = lineRenderer.GetPosition(1) - lineRenderer.GetPosition(0);
    //         objectRigidBody2D.AddForce(force * this.ForceCoefficient);
    //     }
    // }


    public void OnBeginDrag(PointerEventData eventData)
    {
        this.dragStart = Camera.main.ScreenToWorldPoint(eventData.position);
        this.lineObject = new GameObject();
        var lineRenderer = this.lineObject.AddComponent<LineRenderer>();
        lineRenderer.SetPositions(new Vector3[]{ this.ObjectToFire.transform.position, this.ObjectToFire.transform.position });
        lineRenderer.startWidth = lineRenderer.endWidth = 0.1f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        var position = Camera.main.ScreenToWorldPoint(eventData.position);
        var lineRenderer = this.lineObject.GetComponent<LineRenderer>();
        lineRenderer.SetPositions(new Vector3[]{ this.ObjectToFire.transform.position, this.ObjectToFire.transform.position + position - this.dragStart });
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if(this.ObjectToFire != null)
        {
            //var objectRigidBody2D = this.ObjectToFire.GetComponent<Rigidbody2D>();
            var gravityAffected = this.ObjectToFire.GetComponent<GravityAffected>();
            gravityAffected.enabled = true;
            var position = Camera.main.ScreenToWorldPoint(eventData.position);
            var vector = position - this.dragStart;
            gravityAffected.velocity = GameConstants.Instance.GlobalCoefficient * vector * this.ForceCoefficient;

            Destroy(this.lineObject);
            this.lineObject = null;
        }
    }
}