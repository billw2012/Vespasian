using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragToFireWeapon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    private GameObject weaponOrigin = null;
    [SerializeField]
    private GameObject projectilePrefab = null;

    //private bool dragging = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        //this.dragging = true;
        Debug.Log("DragToFireWeapon: dragging started");
    }

    public void OnDrag(PointerEventData eventData)
    {
        // eh?
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("DragToFireWeapon: dragging ended");

        // Calculate the projectile direction
        var weaponOriginPos = this.weaponOrigin.GetComponent<Transform>().position;
        var weaponOriginScreenPos = (Vector2)Camera.main.WorldToScreenPoint(weaponOriginPos);
        var dragEndScreenPos = eventData.position;
        var shootVector = (dragEndScreenPos - weaponOriginScreenPos).normalized;

        Debug.Log($"DragToFireWeapon: weapon screen pos: {weaponOriginScreenPos}, drag end screen pos: {dragEndScreenPos}, shoot vector: {shootVector}");

        // Instantiate the projectile
        var projectile = Instantiate(this.projectilePrefab);
        var projectileRotation = Quaternion.FromToRotation(Vector3.up, shootVector);
        var originSimMovement = this.weaponOrigin.GetComponent<SimMovement>();
        var projectileSimMovement = projectile.GetComponent<SimMovement>();
        projectileSimMovement.alignToVelocity = false;
        projectileSimMovement.SetPositionVelocity(weaponOriginPos, projectileRotation, (Vector2)originSimMovement.velocity);

        //this.dragging = false;
    }
}
