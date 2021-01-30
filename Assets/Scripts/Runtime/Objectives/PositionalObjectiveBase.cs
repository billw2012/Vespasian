using Pixelplacement;
using System;
using UnityEditor;
using UnityEngine;

public abstract class PositionalObjective : Objective
{
    public GameObject objectiveMarkerAsset;
    public GameObject uiIconAsset;
    public Color activeColor = Color.white;
    public Color inactiveColor = Color.clear;
    public Color completionColor = Color.green;

    [NonSerialized]
    public GameObject objectiveMarker;

    private bool completed = false;

    private MaterialPropertyBlock objectiveMarkerPb;
    private LineRenderer lineRenderer;

    private void Start()
    {
        this.objectiveMarker = Instantiate(this.objectiveMarkerAsset);
        this.objectiveMarker.transform.localScale = Vector3.one * this.radius;

        this.lineRenderer = this.objectiveMarker.GetComponent<LineRenderer>();

        this.objectiveMarkerPb = new MaterialPropertyBlock();
    }

    // Use FixedUpdate as we are tracking position of objects that are updated in FixedUpdate
    private void FixedUpdate()
    {
        if (this.objectiveMarker.transform != null && this.target != null)
        {
            this.objectiveMarker.transform.position = this.target.position;
        }

        if (this.lineRenderer.HasPropertyBlock())
        {
            this.lineRenderer.GetPropertyBlock(this.objectiveMarkerPb);
        }

        if(this.active)
        {
            //float anim = 1 - Time.time % 1f; //(Time.time * Mathf.Pow(1 + this.amountDone / this.amountRequired, 2)) % 1f;
            this.lineRenderer.colorGradient = new Gradient {
                colorKeys = new[] {
                    // Progress bar
                    new GradientColorKey(Color.white, Mathf.Clamp01(this.fractionDone + 0.05f * this.fractionDone)),
                    new GradientColorKey(this.completionColor, this.fractionDone),
                    //// Pulse
                    //new GradientColorKey(Color.white, this.fractionDone + anim * (1 - this.fractionDone) - 0.1f),
                    //new GradientColorKey(this.completionColor, this.fractionDone + anim * (1 - this.fractionDone) ),
                    //new GradientColorKey(Color.white, this.fractionDone + anim * (1 - this.fractionDone) + 0.1f),
                },
                alphaKeys = new[] {
                    new GradientAlphaKey(1, 0)
                }
            };
            // This makes a rotating gradient, cool but we going a different way
            //float anim = (Time.time * (0.1f + 3f * this.amountDone / this.amountRequired)) % 1f;

            //var at01 = this.pulseGradient.Evaluate(1 - anim);

            //// rotated gradient
            //this.lineRenderer.colorGradient = new Gradient
            //{
            //    colorKeys = new[] { new GradientColorKey(at01, 0) }
            //        .Concat(this.pulseGradient.colorKeys.Select(k => new GradientColorKey(k.color, (k.time + anim) % 1f)))
            //        .Concat(new[] { new GradientColorKey(at01, 1) })
            //        .ToArray(),
            //    alphaKeys = new[] { new GradientAlphaKey(at01.a, 0) }
            //        .Concat(this.pulseGradient.alphaKeys.Select(k => new GradientAlphaKey(k.alpha, (k.time + anim) % 1f)))
            //        .Concat(new[] { new GradientAlphaKey(at01.a, 1) })
            //        .ToArray(),
            //    mode = this.pulseGradient.mode
            //};
        }
        else
        {
            this.lineRenderer.colorGradient = new Gradient
            {
                colorKeys = new[] {
                    new GradientColorKey(this.inactiveColor, Mathf.Clamp01(this.fractionDone + 0.05f * this.fractionDone)),
                    new GradientColorKey(this.completionColor, this.fractionDone),
                },
                alphaKeys = new[] {
                    new GradientAlphaKey(1, 0)
                }
            };
        }

        this.objectiveMarkerPb.SetColor("_BaseColor", 
            this.active?
            this.activeColor
            :
            this.inactiveColor
            );
        this.lineRenderer.SetPropertyBlock(this.objectiveMarkerPb);

        if (this.complete || this.failed)
        {
            // Hide the objective marker once we are done
            if (!this.completed)
            {
                var markerCircle = this.objectiveMarker.GetComponent<CircleRenderer>();
                Tween.LocalScale(this.objectiveMarker.transform,
                    Vector3.zero, //-Vector3.one * this.radius * 2,
                    duration: 0.2f,
                    delay: 0.1f,
                    easeCurve: Tween.EaseInBack);
                Tween.LocalRotation(this.objectiveMarker.transform,
                    Vector3.forward * 1080f,
                    duration: 0.2f,
                    delay: 0.1f,
                    easeCurve: Tween.EaseInBack,
                    completeCallback: () => this.objectiveMarker.SetActive(false));

                this.completed = true;
            }
        }
        else
        {
            this.UpdateObjective();
        }
    }

    protected abstract void UpdateObjective();

    #region Objective implementation
    public override GameObject uiAsset => this.uiIconAsset;
    #endregion

    public abstract Transform target { get; }
    public abstract float radius { get; }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (this.target != null)
        {
            Handles.color = this.color;
            Handles.matrix = Matrix4x4.Translate(this.target.position);
            Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.radius);
            GUIUtils.Label(Quaternion.Euler(0, 0, this.debugName.GetHashCode() % 90) * Vector2.right * this.radius * 0.95f, this.debugName);
        }
    }
#endif
}
