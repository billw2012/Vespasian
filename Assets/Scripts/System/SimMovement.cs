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
    [Range(0, 50)]
    public float pathWidthScale = 1f;

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

    void Start()
    {
        var simManager = FindObjectOfType<SimManager>();

        this.path = simManager.CreateSectionedSimPath(this.transform.position, this.startVelocity, 5000, this.transform.localScale.x, 500);

        if (this.pathRendererAsset != null)
        {
            this.pathRenderer = Instantiate(this.pathRendererAsset).GetComponent<LineRenderer>();
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

    float rotVelocity;

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
        //this.pathRenderer.startWidth = 0;
        //this.pathRenderer.endWidth = (1 + 9 * this.pathLength / GameConstants.Instance.SimDistanceLimit);
        // Fixed width line in screen space:
        if (this.pathRenderer != null)
        {
            this.pathRenderer.startWidth = this.pathRenderer.endWidth = this.constants.SimLineWidth * this.pathWidthScale;
        }
    }

    void UpdatePath()
    {
        this.sois = this.path.GetFullPathSOIs().ToList();
        this.isPrimaryRelative = this.sois.Count == 1;

        var endPosition = Vector3.zero;
        if (this.pathRenderer != null)
        {
            Vector3[] finalPath;
            if (this.isPrimaryRelative)
            {
                var soi = this.sois.FirstOrDefault();

                if (this.path.crashed)
                {
                    finalPath = soi.relativePath.path.ToArray();
                }
                else
                {
                    float totalAngle = 0;
                    int range = 1;
                    for (; range < soi.relativePath.path.Count && totalAngle < 360f; range++)
                    {
                        totalAngle += Vector2.Angle(soi.relativePath.path[range - 1], soi.relativePath.path[range]);
                    }
                    finalPath = soi.relativePath.path.Take(range).ToArray();
                }

                this.pathRenderer.transform.SetParent(soi.g.GetComponent<Orbit>().position, worldPositionStays: false);
                this.pathRenderer.useWorldSpace = false;
            }
            else
            {
                finalPath = this.path.GetFullPath().ToArray();

                this.pathRenderer.transform.SetParent(null, worldPositionStays: false);
                this.pathRenderer.useWorldSpace = true;
            }
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

        var path = simPath.path.path.AsEnumerable();
        if (path.Count() % 2 == 1)
        {
            path = path.Take(path.Count() - 1);
        }
        this.editorCurrPath = path.ToArray();
        this.editorCrashed = simPath.crashed;
    }
#endif
}