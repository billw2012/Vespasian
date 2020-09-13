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
    public GameObject pathRendererGlobalAsset;
    [Range(0, 50)]
    public float globalPathWidthScale = 1f;
    [Tooltip("Used to render the simulated path in the first soi")]
    public GameObject pathRendererSoiAsset;
    [Range(0, 50)]
    public float soiPathWidthScale = 1f;

    [Tooltip("Used to indicate a predicted crash")]
    public GameObject warningSign;

    public Vector3 simPosition => this.path.position;
    public Vector3 velocity => this.path.velocity;

    [HideInInspector]
    public List<SimModel.SphereOfInfluence> sois = new List<SimModel.SphereOfInfluence>();

    SectionedSimPath path;
    Vector3 force = Vector3.zero;

    LineRenderer pathRendererGlobal;
    LineRenderer pathRendererSoi;

    void OnValidate()
    {
        Assert.IsNotNull(this.constants);
    }

    void Start()
    {
        this.OnValidate();

        var simManager = FindObjectOfType<SimManager>();
        Assert.IsNotNull(simManager);

        this.path = simManager.CreateSectionedSimPath(this.transform.position, this.startVelocity, 100, this.transform.localScale.x, 1000);

        if (this.pathRendererGlobalAsset != null)
        {
            this.pathRendererGlobal = Instantiate(this.pathRendererGlobalAsset).GetComponent<LineRenderer>();
            this.pathRendererGlobal.gameObject.SetActive(false);
        }

        if (this.pathRendererSoiAsset)
        {
            this.pathRendererSoi = Instantiate(this.pathRendererSoiAsset).GetComponent<LineRenderer>();
            this.pathRendererSoi.gameObject.SetActive(false);
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

    public void SimUpdate(float simTime)
    {
        this.path.Step(simTime, this.force);

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
            //var desiredRot = Quaternion.FromToRotation(Vector3.up, this.velocity).eulerAngles.z;
            //var currentRot = rigidBody.rotation.eulerAngles.z;
            //var smoothedRot = Mathf.SmoothDampAngle(currentRot, desiredRot, ref this.rotVelocity, 0.01f, 90);
            //rigidBody.MoveRotation(Quaternion.AngleAxis(smoothedRot, Vector3.forward));

            rigidBody.MoveRotation(Quaternion.FromToRotation(Vector3.up, this.velocity));
        }
    }

    void UpdatePathWidth()
    {
        //this.pathRenderer.startWidth = 0;
        //this.pathRenderer.endWidth = (1 + 9 * this.pathLength / GameConstants.Instance.SimDistanceLimit);
        // Fixed width line in screen space:
        if (this.pathRendererGlobal != null)
        {
            this.pathRendererGlobal.startWidth = this.pathRendererGlobal.endWidth = this.constants.SimLineWidth * this.globalPathWidthScale;
        }
        if (this.pathRendererSoi != null)
        {
            this.pathRendererSoi.startWidth = this.pathRendererSoi.endWidth = this.constants.SimLineWidth * this.soiPathWidthScale;
        }
    }

    void UpdatePath()
    {
        this.sois = this.path.GetFullPathSOIs().ToList();
        var fullPath = this.path.GetFullPath().ToArray();

        if (this.sois.Count == 1/* && primarySoi.relativePath.Count > 1*/)
        {
            if (this.pathRendererGlobal != null)
            {
                var soi = this.sois.FirstOrDefault();

                // TODO: Maybe only show a single orbit? But secondaries can disrupt it so maybe not...
                float totalAngle = 0;
                // Vector2.SignedAngle(Vector2.right, soi.relativePath[0]);
                int range = 1;
                for (; range < soi.relativePath.Count && totalAngle < 360f; range++)
                {
                    totalAngle += Vector2.Angle(soi.relativePath[range - 1], soi.relativePath[range]);
                }
                var conicSection = soi.relativePath.Take(range).ToArray();
                //new List<Vector3> { soi.relativePath[0] };
                //soi.relativePath.TakeWhile(v => Vector2.SignedAngle(Vector2.right, v) - initialAngle < 180);
                this.pathRendererGlobal.positionCount = conicSection.Length;
                // Mathf.Min(primarySoi.relativePath.Count, 500);
                this.pathRendererGlobal.SetPositions(conicSection);
                this.pathRendererGlobal.transform.SetParent(soi.g.GetComponent<Orbit>().position, worldPositionStays: false);
                this.pathRendererGlobal.useWorldSpace = false;
                this.pathRendererGlobal.gameObject.SetActive(true);
            }

            //if (this.pathRendererGlobal != null)
            //{
            //    this.pathRendererGlobal.gameObject.SetActive(false);
            //}
        }
        else
        {
            if (this.pathRendererGlobal != null)
            {
                this.pathRendererGlobal.positionCount = fullPath.Length;
                this.pathRendererGlobal.SetPositions(fullPath.ToArray());
                this.pathRendererGlobal.useWorldSpace = true;
                this.pathRendererGlobal.gameObject.SetActive(true);
            }

            //if (this.pathRendererSoi != null)
            //{
            //    this.pathRendererSoi.gameObject.SetActive(false);
            //}
        }

        if (this.warningSign != null)
        {
            if (this.path.crashed && fullPath.Length > 0)
            {
                var canvas = this.warningSign.GetComponent<Graphic>().canvas;
                if (canvas != null)
                {
                    this.warningSign.SetActive(true);
                    var rectTransform = this.warningSign.GetComponent<RectTransform>();
                    var canvasSafeArea = canvas.ScreenToCanvasRect(Screen.safeArea);
                    var targetCanvasPosition = canvas.WorldToCanvasPosition(fullPath.Last());
                    var clampArea = new Rect(
                        canvasSafeArea.x - rectTransform.rect.x,
                        canvasSafeArea.y - rectTransform.rect.y,
                        canvasSafeArea.width - rectTransform.rect.width,
                        canvasSafeArea.height - rectTransform.rect.height
                    );
                    rectTransform.anchoredPosition = clampArea.ClampToRectOnRay(targetCanvasPosition);
                }
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

        var path = simPath.path.AsEnumerable();
        if (path.Count() % 2 == 1)
        {
            path = path.Take(path.Count() - 1);
        }
        this.editorCurrPath = path.ToArray();
        this.editorCrashed = simPath.crashed;
    }
#endif
}