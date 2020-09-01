using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class SimMovement : MonoBehaviour
{
    public GameConstants constants;

    public Vector3 startVelocity;
    public bool alignToVelocity = true;

    [Tooltip("Used to render the simulated path")]
    public LineRenderer pathRenderer;

    [Tooltip("Used to indicate a predicted crash")]
    public GameObject warningSign;

    public Vector3 simPosition => this.path.position;
    public Vector3 velocity => this.path.velocity;

    [HideInInspector]
    public List<SimModel.SphereOfInfluence> sois = new List<SimModel.SphereOfInfluence>();

    SectionedSimPath path;
    Vector3 force = Vector3.zero;

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
    }

    void Update()
    {
        if (this.pathRenderer != null)
        {
            this.UpdatePath();
            this.UpdatePathWidth();
        }
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
        this.pathRenderer.startWidth = this.pathRenderer.endWidth = this.constants.SimLineWidth;
    }

    void UpdatePath()
    {
        var fullPath = this.path.GetFullPath().ToArray();

        // Resume in main thread
        this.pathRenderer.positionCount = fullPath.Length;
        this.pathRenderer.SetPositions(fullPath.ToArray());
        this.sois = this.path.GetFullPathSOIs().ToList();

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
}