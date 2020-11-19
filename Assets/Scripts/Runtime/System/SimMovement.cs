using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class SimMovement : MonoBehaviour, ISimUpdate
{
    public GameConstants constants;

    public Vector3 startVelocity;
    public bool alignToVelocity = true;

    [Tooltip("Used to render the simulated path sections")]
    public GameObject pathRendererAsset;
    [Tooltip("Used to render markers to show soi closest approaches")]
    public GameObject soiMarkerAsset;

    [Range(0, 5)]
    public float pathWidthScale = 1f;

    public float pathQuality = 0.1f;

    [Tooltip("Used to indicate a predicted crash")]
    public GameObject warningSign;

    public Vector3 simPosition => this.path.position;
    public Vector3 velocity => this.path.velocity;
    public Vector3 relativeVelocity => this.path.relativeVelocity;

    [HideInInspector]
    public List<SimModel.SphereOfInfluence> sois = new List<SimModel.SphereOfInfluence>();

    SectionedSimPath path;
    Vector3 force = Vector3.zero;

    class SoiPathSectionRenderer
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

    readonly List<SoiPathSectionRenderer> pathRenderers = new List<SoiPathSectionRenderer>();

    float rotVelocity;

    void Start()
    {
        this.SimRefresh();
    }

    void Update()
    {
        this.UpdatePath();
        this.UpdatePathWidth();
    }

    void OnDisable()
    {
        foreach (var spr in this.pathRenderers.Where(p => p.valid))
        {
            spr.SetActive(false);
        }
    }

    void OnEnable()
    {
        foreach (var spr in this.pathRenderers.Where(p => p.valid))
        {
            spr.SetActive(true);
        }
    }

    public void SetVelocity(Vector3 velocity)
    {
        this.startVelocity = velocity;
        this.SimRefresh();
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
            float desiredRot = Quaternion.FromToRotation(Vector3.up, this.relativeVelocity).eulerAngles.z;
            float currentRot = rigidBody.rotation.eulerAngles.z;
            float smoothedRot = Mathf.SmoothDampAngle(currentRot, desiredRot, ref this.rotVelocity, 0.01f, 360);
            rigidBody.MoveRotation(Quaternion.AngleAxis(smoothedRot, Vector3.forward));
        }
    }
    public void SimRefresh()
    {
        var simManager = FindObjectOfType<Simulation>();

        this.path = simManager.CreateSectionedSimPath(this.transform.position, this.startVelocity, 5000, this.transform.localScale.x, 500);

        this.sois = new List<SimModel.SphereOfInfluence>();
    }
    #endregion

    void UpdatePathWidth()
    {
        foreach(var sr in this.pathRenderers.Where(p => p.valid))
        {
            sr.UpdatePathWidth(this.constants.SimLineWidth * this.pathWidthScale);
        }
    }

    static readonly Vector3[] EmptyPath = new Vector3[0];

    static Vector3[] Reduce(IList<Vector3> fullPath, float quality)
    {
        int scaling = (int)(1f / quality);
        var finalPath = new Vector3[Mathf.FloorToInt((float)fullPath.Count / scaling)];
        for (int i = 0; i < finalPath.Length; i++)
        {
            finalPath[i] = fullPath[i * scaling];
        }
        return finalPath;
    }

    static Vector3[] ReduceRange(IList<Vector3> fullPath, int start, int end, float quality)
    {
        int scaling = (int)(1f / quality);
        var finalPath = new Vector3[Mathf.FloorToInt((float)(end - start) / scaling)];
        for (int i = 0; i < finalPath.Length; i++)
        {
            finalPath[i] = fullPath[start + i * scaling];
        }
        return finalPath;
    }

    Vector3[] GetPath()
    {
        if (this.sois.Any())
        {
            var g = this.sois.First().g;
            var relativePath = this.path.GetRelativePath(g);
            if(relativePath == null)
            {
                return EmptyPath;
            }

            var relativePathReduced = Reduce(relativePath.positions, this.pathQuality);
            for (int i = 0; i < relativePathReduced.Length; i++)
            {
                relativePathReduced[i] += g.position;
            }
            return relativePathReduced;
        }
        else
        {
            return Reduce(this.path.GetAbsolutePath(), this.pathQuality);
        }
    }

    IEnumerable<(SimModel.SphereOfInfluence soi, Vector3[] path)> GetSOIPaths()
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
                int soiOffset = soiStartTick - primaryRelativePath.startTick;
                int soiDuration = soi.endTick - soiStartTick;
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

    void UpdatePath()
    {
        this.sois = this.path.GetFullPathSOIs().ToList();

        var endPosition = Vector3.zero;
        if (this.pathRendererAsset != null)
        {
            var soiPaths = this.GetSOIPaths().ToList();

            // If we aren't predicted to crash and we only pass one soi, then we 
            // can clip the path to only a single orbit of that soi for neatness
            if (!this.path.crashed && this.sois.Count == 1)
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