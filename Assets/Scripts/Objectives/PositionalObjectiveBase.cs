﻿using Pixelplacement;
using Pixelplacement.TweenSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class PositionalObjective : Objective
{
    public GameObject objectiveMarkerAsset;
    public GameObject uiIconAsset;

    [NonSerialized]
    public GameObject objectiveMarker;

    void Start()
    {
        this.objectiveMarker = Instantiate(this.objectiveMarkerAsset, this.target);
        this.objectiveMarker.transform.localScale = Vector3.one * this.radius;
    }

    // Use FixedUpdate as we are tracking position of objects that are updated in FixedUpdate
    void FixedUpdate()
    {
        if(this.complete)
        {
            return;
        }

        this.UpdateObjective();

        // Hide the objective marker once we are done
        if (this.complete)
        {
            var markerCircle = this.objectiveMarker.GetComponent<CircleRenderer>();
            Tween.Value(markerCircle.degrees, 0,
                v => { markerCircle.degrees = v; markerCircle.UpdateCircle(); },
                duration: 0.2f,
                delay: 0.1f,
                easeCurve: Tween.EaseInBack,
                completeCallback: () => this.objectiveMarker.SetActive(false));
            Tween.LocalScale(this.objectiveMarker.transform,
                Vector3.one * this.radius * 3,
                duration: 0.2f,
                delay: 0.1f,
                easeCurve: Tween.EaseInBack,
                completeCallback: () => this.objectiveMarker.SetActive(false));
        }
    }

    protected abstract void UpdateObjective();

    #region Objective implementation
    public override GameObject uiAsset => this.uiIconAsset;
    #endregion

    public abstract Transform target { get; }
    public abstract float radius { get; }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (this.target != null)
        {
            Handles.color = this.color;
            Handles.DrawWireDisc(this.target.position, Vector3.forward, this.radius);
            Handles.Label(this.target.position + Vector3.up * this.radius, this.debugName);
        }
    }
#endif
}
