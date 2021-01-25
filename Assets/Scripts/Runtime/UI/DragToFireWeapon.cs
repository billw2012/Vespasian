using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragToFireWeapon : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [SerializeField]
    private GameObject weaponOrigin = null;
    [SerializeField]
    private GameObject projectilePrefab = null;

    [SerializeField]
    private LineRenderer fireDirectionLineRenderer;

    //  We still must keep this bool,
    // because OnDrag is not called when we have started dragging
    // and keep dragging but don't move the mouse any more
    private bool dragging = false;

    private Vector2 lastOnDragPos;

    void Start()
    {
        this.fireDirectionLineRenderer.enabled = false;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("DragToFireWeapon: dragging started");
        this.fireDirectionLineRenderer.enabled = true;
        this.dragging = true;
    }

    void Update()
    {
        if (this.dragging)
        {
            var touchWorldPos = this.getCursorWorldPos(this.lastOnDragPos);
            Vector3[] linePositions =
            {
                this.weaponOrigin.transform.position,
                touchWorldPos
            };
            linePositions[0].z = 0;
            linePositions[1].z = 0;
            this.fireDirectionLineRenderer.SetPositions(linePositions);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        this.lastOnDragPos = eventData.position;
    }

    private Vector3 getCursorWorldPos(Vector2 position)
    {
        var cursorScreenPos3D = new Vector3(position.x, position.y, 0);
        return Camera.main.ScreenToWorldPoint(cursorScreenPos3D);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("DragToFireWeapon: dragging ended");

        // Calculate the projectile direction
        var cursorWorldPos = this.getCursorWorldPos(eventData.position);
        var weaponOriginPos = this.weaponOrigin.GetComponent<Transform>().position;
        var shootVector = cursorWorldPos - weaponOriginPos;
        shootVector.z = 0;
        shootVector = shootVector.normalized;

        Debug.Log($"DragToFireWeapon: shoot vector: {shootVector}");

        // Instantiate the projectile
        var projectile = Instantiate(this.projectilePrefab);
        var projectileRotation = Quaternion.FromToRotation(Vector3.up, shootVector);
        var originSimMovement = this.weaponOrigin.GetComponent<SimMovement>();
        var projectileSimMovement = projectile.GetComponent<SimMovement>();
        projectileSimMovement.alignToVelocity = false;
        projectileSimMovement.SetPositionVelocity(weaponOriginPos, projectileRotation, (Vector2)originSimMovement.velocity);

        this.dragging = false;
        this.fireDirectionLineRenderer.enabled = false;
    }
}
