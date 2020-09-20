using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class SimMovement : MonoBehaviour
{
    public GameConstants constants;

    public Vector3 startVelocity;
    public bool alignToVelocity = true;

    [Tooltip("Used to render the global simulated path")]
    public GameObject pathRendererAsset;
    [Range(0, 5)]
    public float pathWidthScale = 1f;

    [Range(0, 5)]
    public float pathSOIBlending = 1f;

    [Tooltip("Used to indicate a predicted crash")]
    public GameObject warningSign;

    public Vector3 simPosition => this.path.position;
    public Vector3 velocity => this.path.velocity;
    public bool isPrimaryRelative;
    public Vector3 relativeVelocity => this.isPrimaryRelative? this.path.relativeVelocity : this.path.velocity;

    [HideInInspector]
    public List<SimModel.SphereOfInfluence> sois = new List<SimModel.SphereOfInfluence>();

    SectionedSimPath path;
    Vector3 force = Vector3.zero;

    LineRenderer pathRenderer;
    MaterialPropertyBlock pathRendererUVScaling;

    float rotVelocity;

    void Start()
    {
        var simManager = FindObjectOfType<SimManager>();

        this.path = simManager.CreateSectionedSimPath(this.transform.position, this.startVelocity, 5000, this.transform.localScale.x, 500);

        if (this.pathRendererAsset != null)
        {
            this.pathRenderer = Instantiate(this.pathRendererAsset).GetComponent<LineRenderer>();

            this.pathRendererUVScaling = new MaterialPropertyBlock();
        }
    }

    void Update()
    {
        this.UpdatePath();
        this.UpdatePathWidth();
    }

    public void AddForce(Vector3 force)
    {
        this.force += force;
    }

    public void SimUpdate(int simTick)
    {
        this.path.Step(simTick, this.force);

        this.force = Vector3.zero;

        var rigidBody = this.GetComponent<Rigidbody>();
        this.GetComponent<Rigidbody>().MovePosition(this.simPosition);

        if (this.alignToVelocity)
        {
            // TODO: rotation needs to be smoothed, but this commented out method results in rotation
            // while following the current orbit lagging.
            // (perhaps even limiting them to the same magnitude?)
            // Smooth rotation slightly to avoid random flipping. Smoothing should not be noticeable in
            // normal play.
            var desiredRot = Quaternion.FromToRotation(Vector3.up, this.relativeVelocity).eulerAngles.z;
            var currentRot = rigidBody.rotation.eulerAngles.z;
            var smoothedRot = Mathf.SmoothDampAngle(currentRot, desiredRot, ref this.rotVelocity, 0.01f, 360);
            rigidBody.MoveRotation(Quaternion.AngleAxis(smoothedRot, Vector3.forward));

            //rigidBody.MoveRotation(Quaternion.FromToRotation(Vector3.up, this.relativeVelocity));
        }
    }

    void UpdatePathWidth()
    {
        if (this.pathRenderer != null)
        {
            this.pathRenderer.startWidth = this.pathRenderer.endWidth = this.constants.SimLineWidth * this.pathWidthScale;
            float ratio = (float)this.pathRenderer.sharedMaterial.mainTexture.height / this.pathRenderer.sharedMaterial.mainTexture.width;

            this.pathRendererUVScaling.SetVector("_UVScaling", new Vector2(ratio / (this.constants.SimLineWidth * this.pathWidthScale), 1));
            this.pathRenderer.SetPropertyBlock(this.pathRendererUVScaling);
        }
    }

    void UpdatePath()
    {
        this.sois = this.path.GetFullPathSOIs().ToList();
        this.isPrimaryRelative = this.sois.Count > 0;

        var endPosition = Vector3.zero;
        if (this.pathRenderer != null)
        {
            Vector3[] finalPath;

            // Lerped path:
            // - Global positions
            // Position is sum of weighted relative positions relative to forces..
            //if (this.isPrimaryRelative)
            //{
            var fullPath = this.path.GetWeightedPath(this.pathSOIBlending);
            const int Scaling = 10;
            finalPath = new Vector3[Mathf.FloorToInt((float)fullPath.Length / Scaling)];
            for (int i = 0; i < finalPath.Length; i++)
            {
                finalPath[i] = fullPath[i * Scaling];
            }
            this.pathRenderer.transform.SetParent(null, worldPositionStays: false);
            this.pathRenderer.useWorldSpace = true;
            if(!this.path.crashed && this.sois.Count == 1)
            {
                var soiPos = this.sois.First().g.position;
                float totalAngle = 0;
                int range = 1;
                for (; range < finalPath.Length && totalAngle < 360f; range++)
                {
                    totalAngle += Vector2.Angle(finalPath[range - 1] - soiPos, finalPath[range] - soiPos);
                }
                finalPath = finalPath.Take(range).ToArray();
            }
            //var relativePath = this.path.GetRelativePath(soi.g);
            //if (this.path.crashed)
            //{
            //    finalPath = relativePath.ToArray(); // soi.relativePath.path.ToArray();
            //}
            //else
            //{
            //    float totalAngle = 0;
            //    int range = 1;
            //    for (; range < relativePath.Count && totalAngle < 360f; range++)
            //    {
            //        totalAngle += Vector2.Angle(relativePath[range - 1], relativePath[range]);
            //    }
            //    finalPath = relativePath.Take(range).ToArray();
            //}

            //this.pathRenderer.transform.SetParent(soi.g.GetComponent<Orbit>().position, worldPositionStays: false);
            //this.pathRenderer.useWorldSpace = false;
            //}
            //else
            //{
            //    finalPath = this.path.GetFullPath().ToArray();
            //
            //    this.pathRenderer.transform.SetParent(null, worldPositionStays: false);
            //    this.pathRenderer.useWorldSpace = true;
            //}
            endPosition = this.pathRenderer.transform.localToWorldMatrix.MultiplyPoint(finalPath.LastOrDefault());
            this.pathRenderer.positionCount = finalPath.Length;
            this.pathRenderer.SetPositions(finalPath);
        }

        if (this.warningSign != null)
        {
            if (this.path.crashed)
            {
                var canvas = this.warningSign.GetComponent<Graphic>().canvas;
                this.warningSign.SetActive(true);
                var rectTransform = this.warningSign.GetComponent<RectTransform>();
                var canvasSafeArea = canvas.ScreenToCanvasRect(Screen.safeArea);
                var targetCanvasPosition = canvas.WorldToCanvasPosition(endPosition);
                var clampArea = new Rect(
                    canvasSafeArea.x - rectTransform.rect.x,
                    canvasSafeArea.y - rectTransform.rect.y,
                    canvasSafeArea.width - rectTransform.rect.width,
                    canvasSafeArea.height - rectTransform.rect.height
                );
                rectTransform.anchoredPosition = clampArea.ClampToRectOnRay(targetCanvasPosition);
            }
            else
            {
                this.warningSign.SetActive(false);
            }
        }
    }

#if UNITY_EDITOR
    [NonSerialized]
    public Vector3[] editorCurrPath;
    [NonSerialized]
    public bool editorCrashed;

    public async Task EditorUpdatePathAsync(SimModel simModel)
    {
        var simPath = await simModel.CalculateSimPathAsync(
            this.transform.position,
            this.startVelocity,
            0,
            Time.fixedDeltaTime,
            5000,
            1,
            this.constants.GravitationalConstant,
            this.constants.GravitationalRescaling
        );

        var path = simPath.pathSection.positions.AsEnumerable();
        if (path.Count() % 2 == 1)
        {
            path = path.Take(path.Count() - 1);
        }
        this.editorCurrPath = path.ToArray();
        this.editorCrashed = simPath.crashed;
    }
#endif
}