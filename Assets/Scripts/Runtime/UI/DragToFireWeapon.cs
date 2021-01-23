using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragToFireWeapon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject weaponOrigin;

    public GameObject projectilePrefab;

    private bool dragging = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    //void Update()
    //{

    //}

    public void OnBeginDrag(PointerEventData eventData)
    {
        this.dragging = true;
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
        var canvas = this.GetComponentInParent<Canvas>();
        var weaponOriginPos = this.weaponOrigin.GetComponent<Transform>().position;
        var weaponOriginScreenPos3D = Camera.main.WorldToScreenPoint(weaponOriginPos);
        var weaponOriginScreenPos = new Vector2(weaponOriginScreenPos3D.x, weaponOriginScreenPos3D.y);
        var dragEndScreenPos = eventData.position;
        var shootVector2D = (dragEndScreenPos - weaponOriginScreenPos);
        shootVector2D.Normalize();
        Vector3 shootVector3D = new Vector3(shootVector2D.x, shootVector2D.y, 0);

        Debug.Log($"DragToFireWeapon: weapon screen pos: {weaponOriginScreenPos}, drag end screen pos: {dragEndScreenPos}, shoot vector: {shootVector3D}");

        // Instantiate the projectile
        GameObject projectile = Instantiate(projectilePrefab);
        Quaternion projectileRotation = Quaternion.FromToRotation(Vector3.up, shootVector3D);
        var originSimMovement = this.weaponOrigin.GetComponent<SimMovement>();
        var projectileSimMovement = projectile.GetComponent<SimMovement>();
        projectileSimMovement.alignToVelocity = false;
        projectileSimMovement.SetPositionVelocity(weaponOriginPos, projectileRotation, originSimMovement.velocity);

        this.dragging = false;
    }
}
