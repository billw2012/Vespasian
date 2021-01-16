using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Describes and updates a simulated path for an object moving under external forces.
/// It generates the path ahead of time as long as no direct force is applied.
/// </summary>
public class SimMovement : MonoBehaviour, ISimUpdate
{
    public GameConstants constants;

    public Rigidbody controlledRigidbody;

    public Vector3 startVelocity;
    public bool alignToVelocity = true;

    [Tooltip("Used to render the simulated path sections")]
    public GameObject pathRendererAsset;
    [Tooltip("Used to render markers to show soi closest approaches")]
    public GameObject soiMarkerAsset;

    public bool soiRelativePaths;

    [Range(0, 5)]
    public float pathWidthScale = 1f;

    public float pathQuality = 1f;

    [Tooltip("Used to indicate a predicted crash")]
    public GameObject warningSign;

    public float collisionRadius = 0.5f;

    public UnityEvent OnCrashed;

    public Vector3 simPosition => this.path.position;
    public Vector3 velocity => this.path.velocity;
    public Vector3 relativeVelocity => this.path.relativeVelocity;

    [HideInInspector]
    public List<SimModel.SphereOfInfluence> sois = new List<SimModel.SphereOfInfluence>();

    private SectionedSimPath path;
    private Vector3 force = Vector3.zero;

    private class SoiPathSectionRenderer
    {
        public LineRenderer lineRenderer;
        public MaterialPropertyBlock lineRendererMB;
        public Renderer soiMarker;
        public MaterialPropertyBlock soiMarkerMB;

        public bool valid => this.lineRenderer != null && this.soiMarker != null;

        public SoiPathSectionRenderer(GameObject lineRendererPrefab, GameObject soiMarkerPrefab)
        {
            this.lineRenderer = Instantiate(lineRendererPrefab).GetComponent<LineRenderer>();
            this.soiMarker = Instantiate(soiMarkerPrefab).GetComponentInChildren<Renderer>();
            this.lineRendererMB = new MaterialPropertyBlock();
            this.soiMarkerMB = new MaterialPropertyBlock();
            this.SetActive(false);
        }

        public void SetActive(bool active)
        {
            this.lineRenderer.gameObject.SetActive(active);
            this.soiMarker.gameObject.SetActive(active);
        }

        public void UpdatePathWidth(float width)
        {
            this.lineRenderer.startWidth = this.lineRenderer.endWidth = width;
            float ratio = (float)this.lineRenderer.sharedMaterial.mainTexture.height / this.lineRenderer.sharedMaterial.mainTexture.width;

            this.lineRendererMB.SetVector("_UVScaling", new Vector2(ratio / width, 1));
            this.lineRenderer.SetPropertyBlock(this.lineRendererMB);
            this.soiMarker.transform.localScale = Vector3.one * width;
        }

        public void Update(SimModel.SphereOfInfluence soi, Vector3[] path)
        {
            this.lineRenderer.gameObject.SetActive(true);
            this.lineRenderer.positionCount = path.Length;
            this.lineRenderer.SetPositions(path);
            if (soi != null)
            {
                this.lineRendererMB.SetVector("_BorderColor", soi.g.color);
                this.lineRenderer.SetPropertyBlock(this.lineRendererMB);
                this.soiMarkerMB.SetVector("_BorderColor", soi.g.color);
                this.soiMarker.SetPropertyBlock(this.soiMarkerMB);

                this.soiMarker.transform.position = soi.maxForcePosition;
                this.soiMarker.gameObject.SetActive(true);
            }
            else
            {
                this.lineRendererMB.SetVector("_BorderColor", Color.white);
                this.lineRenderer.SetPropertyBlock(this.lineRendererMB);
                this.soiMarker.gameObject.SetActive(false);
            }
        }
    }

    private readonly List<SoiPathSectionRenderer> pathRenderers = new List<SoiPathSectionRenderer>();

    private float rotVelocity;

    // private void Start()
    // {
    //     this.SimRefresh(FindObjectOfType<Simulation>());
    // }

    private void Update()
    {
        this.UpdatePath();
        this.UpdatePathWidth();
    }

    private void OnDisable()
    {
        foreach (var spr in this.pathRenderers.Where(p => p.valid))
        {
            spr.SetActive(false);
        }
    }

    private void OnEnable()
    {
        foreach (var spr in this.pathRenderers.Where(p => p.valid))
        {
            spr.SetActive(true);
        }
    }

    public void SetVelocity(Vector3 velocity)
    {
        this.startVelocity = velocity;
        this.force = Vector3.zero;
        this.SimRefresh(FindObjectOfType<Simulation>());
    }

    public void SetPositionVelocity(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        this.transform.position = position;
        this.transform.rotation = rotation;
        this.SetVelocity(velocity);
    }

    public void AddForce(Vector3 force)
    {
        this.force += force;
    }

    #region ISimUpdate
    public void SimUpdate(Simulation simulation, int simTick, int timeStep)
    {
        if (!this.path.Step(simTick, this.force, timeStep))
        {
            this.OnCrashed?.Invoke();
        }

        this.force = Vector3.zero;

        this.controlledRigidbody.MovePosition(this.simPosition);

        if (this.alignToVelocity)
        {
            // TODO: rotation needs to be smoothed, but this commented out method results in rotation
            // while following the current orbit lagging.
            // (perhaps even limiting them to the same magnitude?)
            // Smooth rotation slightly to avoid random flipping. Smoothing should not be noticeable in
            // normal play.
            float desiredRot = Quaternion.FromToRotation(Vector3.up, this.relativeVelocity).eulerAngles.z;
            float currentRot = this.controlledRigidbody.rotation.eulerAngles.z;
            float smoothedRot = Mathf.SmoothDampAngle(currentRot, desiredRot, ref this.rotVelocity, 0.01f, 360 * timeStep, deltaTime: Time.fixedDeltaTime * timeStep);
            // this.controlledRigidbody.rotation = Quaternion.AngleAxis(smoothedRot, Vector3.forward);
            this.controlledRigidbody.MoveRotation(Quaternion.AngleAxis(smoothedRot, Vector3.forward));
        }
    }
    
    public void SimRefresh(Simulation simulation)
    {
        this.path = simulation.CreateSectionedSimPath(this.transform.position, this.startVelocity, 20000, this.collisionRadius, 2000);
        this.sois = new List<SimModel.SphereOfInfluence>();
    }
    #endregion

    private void UpdatePathWidth()
    {
        foreach(var sr in this.pathRenderers.Where(p => p.valid))
        {
            sr.UpdatePathWidth(this.constants.SimLineWidth * this.pathWidthScale);
        }
    }

    private static readonly Vector3[] EmptyPath = new Vector3[0];

    private static Vector3[] Reduce(IList<Vector3> fullPath, float quality)
    {
        int scaling = (int)(1f / quality);
        var finalPath = new Vector3[Mathf.FloorToInt((float)fullPath.Count / scaling)];
        for (int i = 0; i < finalPath.Length; i++)
        {
            finalPath[i] = fullPath[i * scaling];
        }
        return finalPath;
    }

    private static Vector3[] ReduceRange(IList<Vector3> fullPath, int start, int end, float quality)
    {
        int scaling = (int)(1f / quality);
        var finalPath = new Vector3[Mathf.FloorToInt((float)(end - start) / scaling)];
        for (int i = 0; i < finalPath.Length && start + i * scaling < fullPath.Count; i++)
        {
            finalPath[i] = fullPath[start + i * scaling];
        }
        return finalPath;
    }

    private IEnumerable<(SimModel.SphereOfInfluence soi, Vector3[] path)> GetSOIPathsPrimaryRelative()
    {
        var soiPaths = new List<(SimModel.SphereOfInfluence soi, Vector3[] path)>();
        if (this.sois.Any())
        {
            var primaryGravitySource = this.sois.First().g;
            var primaryRelativePath = this.path.GetRelativePath(primaryGravitySource);

            foreach (var soi in this.sois)
            {
                // Sometimes soi can start before the existing path does, so we need to ensure we don't try and index negative values
                int soiStartTick = Mathf.Max(primaryRelativePath.startTick, soi.startTick);
                // 0 based offset of the (clamped) soi start in the path positions array
                int soiOffset = Mathf.Clamp((soiStartTick - primaryRelativePath.startTick) / primaryRelativePath.tickStep, 0, primaryRelativePath.positions.Count - 1);
                int soiDuration = Mathf.Clamp((soi.endTick - soiStartTick) / primaryRelativePath.tickStep, 0, primaryRelativePath.positions.Count - 1);
                var relativePath = ReduceRange(primaryRelativePath.positions, soiOffset, soiOffset + soiDuration, this.pathQuality);
                for (int i = 0; i < relativePath.Count(); i++)
                {
                    relativePath[i] += primaryGravitySource.position;
                }
                soiPaths.Add((soi, relativePath));
            }
        }
        else
        {
            soiPaths.Add((null, Reduce(this.path.GetAbsolutePath(), this.pathQuality)));
        }
        return soiPaths;
    }

    private IEnumerable<(SimModel.SphereOfInfluence soi, Vector3[] path)> GetSOIPathsSOIRelative()
    {
        var soiPaths = new List<(SimModel.SphereOfInfluence soi, Vector3[] path)>();
        if (this.sois.Any())
        {
            foreach (var soi in this.sois)
            {
                var soiGravitySource = soi.g;
                var soiRelativePath = this.path.GetRelativePath(soiGravitySource);

                // Sometimes soi can start before the existing path does, so we need to ensure we don't try and index negative values
                int soiStartTick = Mathf.Max(soiRelativePath.startTick, soi.startTick);
                // 0 based offset of the (clamped) soi start in the path positions array
                int soiOffset = Mathf.Clamp((soiStartTick - soiRelativePath.startTick) / soiRelativePath.tickStep, 0, soiRelativePath.positions.Count - 1);
                int soiDuration = Mathf.Clamp((soi.endTick - soiStartTick) / soiRelativePath.tickStep, 0, soiRelativePath.positions.Count - 1);
                var relativePath = ReduceRange(soiRelativePath.positions, soiOffset, soiOffset + soiDuration, this.pathQuality);
                var relativePos = soi == this.sois.First() ? soiGravitySource.position : soi.maxForcePosition;
                for (int i = 0; i < relativePath.Count(); i++)
                {
                    relativePath[i] += relativePos;
                }
                soiPaths.Add((soi, relativePath));
            }
        }
        else
        {
            soiPaths.Add((null, Reduce(this.path.GetAbsolutePath(), this.pathQuality)));
        }
        return soiPaths;
    }
    private void UpdatePath()
    {
        if (this.path == null)
        {
            return;
        }
        
        this.sois = this.path.GetFullPathSOIs().ToList();

        var endPosition = Vector3.zero;
        if (this.pathRendererAsset != null && this.soiMarkerAsset != null)
        {
            var soiPaths = this.soiRelativePaths ? this.GetSOIPathsSOIRelative().ToList() : this.GetSOIPathsPrimaryRelative().ToList();

            // If we aren't predicted to crash and we only pass one soi, then we 
            // can clip the path to only a single orbit of that soi for neatness
            if (!this.path.willCrash && this.sois.Count == 1)
            {
                var primarySoi = soiPaths[0];
                var soiPos = primarySoi.soi.g.position;
                float totalAngle = 0;
                int range = 1;
                for (; range < primarySoi.path.Length && totalAngle < 360f; range++)
                {
                    totalAngle += Vector2.Angle(primarySoi.path[range - 1] - soiPos, primarySoi.path[range] - soiPos);
                }
                soiPaths[0] = (primarySoi.soi, primarySoi.path.Take(range).ToArray());
            }

            // Update the renderers, adding new ones if necessary
            for (int i = 0; i < soiPaths.Count; i++)
            {
                if(this.pathRenderers.Count <= i)
                {
                    this.pathRenderers.Add(new SoiPathSectionRenderer(this.pathRendererAsset, this.soiMarkerAsset));
                }
                var finalPath = soiPaths[i].path;
                this.pathRenderers[i].Update(soiPaths[i].soi, finalPath);
            }

            endPosition = soiPaths.Last().path.LastOrDefault();

            // Disable any superfluous renderers
            for (int i = soiPaths.Count(); i < this.pathRenderers.Count; i++)
            {
                this.pathRenderers[i].SetActive(false);
            }
        }

        if (this.warningSign != null)
        {
            if (this.path.willCrash)
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
        this.editorCrashed = simPath.willCrash;
    }
#endif
}