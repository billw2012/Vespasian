using Pixelplacement;
using Pixelplacement.TweenSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class PositionalObjective : Objective
{
    public GameObject objectiveMarkerAsset;
    public GameObject uiIconAsset;

    GameObject objectiveMarker;
    //TweenBase activeAnim;

    void Start()
    {
        this.objectiveMarker = Instantiate(this.objectiveMarkerAsset, this.target);
        this.objectiveMarker.transform.localScale = Vector3.one * this.radius;
        //this.activeAnim = Tween.LocalScale(this.objectiveMarker.transform,
        //    startValue: Vector3.one * this.radius,
        //    endValue: Vector3.one * this.radius * 0.95f,
        //    duration: 0.5f,
        //    delay: 0,
        //    loop: Tween.LoopType.Loop,
        //    easeCurve: Tween.EaseOut);
    }

    // Use FixedUpdate as we are tracking position of objects that are updated in FixedUpdate
    void FixedUpdate()
    {
        if(this.complete)
        {
            return;
        }

        this.UpdateObjective();

        //if(this.active && this.activeAnim.Status != Tween.TweenStatus.Running)
        //{
        //    this.activeAnim.Start();
        //}
        //else if (!this.active && this.activeAnim.Status == Tween.TweenStatus.Running)
        //{
        //    this.activeAnim.Cancel();
        //}

        // Hide the objective marker once we are done
        if (this.complete)
        {
            //this.activeAnim.Stop();
            Tween.LocalScale(this.objectiveMarker.transform,
                Vector3.one * this.radius * 10,
                0.7f,
                0.5f,
                Tween.EaseInBack,
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
