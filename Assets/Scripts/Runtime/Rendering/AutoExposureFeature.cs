using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

/// <summary>
/// URP Renderer Feature that implements luminance-histogram-based auto exposure.
/// Add this to the URP Renderer asset (Assets/Settings/URP.asset) via the Inspector,
/// assign the compute shader and volume profile, then tune the parameters.
///
/// Each frame: blit camera → 256×144 sample RT, run two compute passes
/// (histogram build + percentile average), async-read the result, then smoothly
/// adapt ColorAdjustments.postExposure on the assigned Volume Profile over time.
/// </summary>
[Serializable]
public class AutoExposureFeature : ScriptableRendererFeature
{
    [Serializable]
    public class Settings
    {
        // Volume is resolved at runtime — ScriptableRendererFeature is an asset and cannot hold scene references.

        [Header("Histogram Range")]
        [Tooltip("Darkest scene luminance (EV) mapped to histogram bin 1. Deep space ≈ -10.")]
        public float histogramMinEV = -10f;
        [Tooltip("Brightest scene luminance (EV) mapped to histogram bin 255. Near a star ≈ 4.")]
        public float histogramMaxEV = 4f;
        [Range(0f, 0.49f)]
        [Tooltip("Fraction of darkest pixels excluded from the average.")]
        public float lowPercentile = 0.05f;
        [Range(0.51f, 1f)]
        [Tooltip("Fraction of brightest pixels excluded from the average.")]
        public float highPercentile = 0.95f;

        [Header("Output Limits")]
        public float minEV = -3f;
        public float maxEV = 3f;
        [Tooltip("Manual EV offset. Positive = brighter image target.")]
        public float exposureCompensation;

        [Header("Adaptation Speed")]
        [Tooltip("EV/s adapting toward darker exposure (into a bright scene).")]
        public float adaptSpeedUp = 3f;
        [Tooltip("EV/s adapting toward brighter exposure (into a dark scene).")]
        public float adaptSpeedDown = 1f;
    }

    public Settings settings = new Settings();

    [Tooltip("Assign Assets/Shaders/AutoExposure.compute here.")]
    public ComputeShader computeShader;

    [Header("Debug")]
    [Tooltip("Show a luminance histogram overlay in the bottom-left corner.")]
    public bool debugHistogram;

    private AutoExposurePass      m_Pass;
    private HistogramDebugUI      m_DebugUI;

    public override void Create()
    {
        m_Pass = new AutoExposurePass(computeShader, settings)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (computeShader == null)
            return;
        if (renderingData.cameraData.cameraType == CameraType.Reflection)
            return;

        m_Pass.DebugEnabled = debugHistogram;
        renderer.EnqueuePass(m_Pass);

        // Lazily spawn / destroy the IMGUI debug overlay
        if (debugHistogram && m_DebugUI == null)
        {
            var go = new GameObject("[AutoExposureDebug]")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            m_DebugUI = go.AddComponent<HistogramDebugUI>();
            m_DebugUI.Init(this);
        }
        else if (!debugHistogram && m_DebugUI != null)
        {
            CoreUtils.Destroy(m_DebugUI.gameObject);
            m_DebugUI = null;
        }
    }

    protected override void Dispose(bool disposing)
    {
        m_Pass?.Dispose();
        m_Pass = null;
        if (m_DebugUI != null)
        {
            CoreUtils.Destroy(m_DebugUI.gameObject);
            m_DebugUI = null;
        }
    }

    // ── Debug IMGUI overlay ───────────────────────────────────────────────────

    private class HistogramDebugUI : MonoBehaviour
    {
        private const int k_TexW   = 256;
        private const int k_TexH   = 64;
        private const int k_DrawW  = 512;   // screen pixels wide
        private const int k_DrawH  = 128;  // screen pixels tall (bars only)
        private const int k_Margin = 16;
        private const int k_LabelH = 18;

        private AutoExposureFeature m_Feature;
        private Texture2D           m_Tex;
        private float               m_LastTargetEV;
        private GUIStyle            m_LabelStyle;

        public void Init(AutoExposureFeature feature)
        {
            m_Feature = feature;
            m_Tex     = new Texture2D(k_TexW, k_TexH, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode   = TextureWrapMode.Clamp
            };
            // Start with a placeholder so the box is visible even before first readback
            FillSolidColor(new Color(0.15f, 0.15f, 0.15f, 1f));
        }

        private void OnGUI()
        {
            if (m_Feature == null || !m_Feature.debugHistogram) return;

            if (m_LabelStyle == null)
            {
                m_LabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize  = 10,
                    alignment = TextAnchor.MiddleLeft
                };
                m_LabelStyle.normal.textColor = Color.white;
            }

            var hist = m_Feature.m_Pass?.HistogramData;
            float logLum   = m_Feature.m_Pass?.TargetLogLum ?? 0f;
            var   s        = m_Feature.settings;
            float targetEV = Mathf.Clamp(s.exposureCompensation - logLum, s.minEV, s.maxEV);

            // Rebuild texture every frame (data is updated in-place via CopyTo)
            if (hist != null)
                RebuildTexture(hist);

            // Layout
            int totalH = k_LabelH + k_DrawH + k_Margin;
            var boxRect   = new Rect(k_Margin, Screen.height - totalH - k_Margin,
                                     k_DrawW + k_Margin * 2, totalH + k_Margin);
            var labelRect = new Rect(boxRect.x + 6, boxRect.y + 4, boxRect.width - 8, k_LabelH);
            var histRect  = new Rect(boxRect.x + k_Margin / 2,
                                     labelRect.yMax + 2, k_DrawW, k_DrawH);

            // Background box
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(boxRect, Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Border
            DrawBorder(boxRect, new Color(0.4f, 0.4f, 0.4f, 0.9f));

            // Label
            string label = hist != null
                ? $"Auto Exposure  |  logLum: {logLum:F2}  targetEV: {targetEV:F2}  currentEV: {m_Feature.m_Pass.CurrentEV:F2}"
                : "Auto Exposure  (waiting for GPU readback…)";
            GUI.Label(labelRect, label, m_LabelStyle);

            // Histogram texture
            GUI.DrawTexture(histRect, m_Tex);

            // Percentile marker lines
            if (hist != null)
            {
                float lx = histRect.x + m_Feature.settings.lowPercentile  * k_DrawW;
                float hx = histRect.x + m_Feature.settings.highPercentile * k_DrawW;
                DrawVLine(lx, histRect.y, histRect.yMax, new Color(0.2f, 1f, 0.2f, 0.9f));
                DrawVLine(hx, histRect.y, histRect.yMax, new Color(1f, 0.3f, 0.3f, 0.9f));
            }
        }

        private void RebuildTexture(uint[] hist)
        {
            uint maxVal = 1u;
            for (int i = 1; i < hist.Length; i++)
                if (hist[i] > maxVal) maxVal = hist[i];

            var pixels = new Color32[k_TexW * k_TexH];
            for (int x = 0; x < k_TexW; x++)
            {
                float t   = hist[x] / (float)maxVal;
                int   barH = Mathf.RoundToInt(t * k_TexH);

                // Gradient: shadows = blue, midtones = grey, highlights = yellow
                float hue    = x / (float)(k_TexW - 1);
                Color barCol = Color.Lerp(new Color(0.25f, 0.45f, 0.9f),
                                          new Color(1.0f,  0.85f, 0.2f), hue);
                Color bgCol  = new Color(0.12f, 0.12f, 0.12f, 1f);

                for (int y = 0; y < k_TexH; y++)
                {
                    // y=0 is the bottom of the texture in Unity (OpenGL convention).
                    // GUI.DrawTexture draws y=0 at the TOP of the rect on Windows/DX,
                    // so flip: fill from the top down (y = k_TexH-1 downward = bar).
                    bool inBar = y >= (k_TexH - barH);
                    Color32 c  = inBar ? (Color32)barCol : (Color32)bgCol;
                    pixels[x + y * k_TexW] = c;
                }
            }

            m_Tex.SetPixels32(pixels);
            m_Tex.Apply(false);
        }

        private void FillSolidColor(Color color)
        {
            var pixels = new Color32[k_TexW * k_TexH];
            Color32 c  = color;
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            m_Tex.SetPixels32(pixels);
            m_Tex.Apply(false);
        }

        private static void DrawBorder(Rect r, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x,              r.y,              r.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,              r.yMax - 1f,      r.width, 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.x,              r.y,              1f, r.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(r.xMax - 1f,     r.y,              1f, r.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawVLine(float x, float y0, float y1, Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(x - 0.5f, y0, 1f, y1 - y0), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void OnDestroy()
        {
            if (m_Tex != null) Destroy(m_Tex);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private sealed class AutoExposurePass : ScriptableRenderPass, IDisposable
    {
        private const string k_Tag         = "AutoExposure";
        private const int    k_SampleW     = 256;
        private const int    k_SampleH     = 144;
        private const int    k_Bins        = 256;
        private const int    k_TotalPixels = k_SampleW * k_SampleH;

        private readonly int k_KernelClear;
        private readonly int k_KernelHistogram;
        private readonly int k_KernelAverage;

        private static readonly int ID_Source         = Shader.PropertyToID("_Source");
        private static readonly int ID_Histogram      = Shader.PropertyToID("_Histogram");
        private static readonly int ID_AverageLogLum  = Shader.PropertyToID("_AverageLogLum");
        private static readonly int ID_MinLogLum      = Shader.PropertyToID("_MinLogLum");
        private static readonly int ID_InvLogLumRange = Shader.PropertyToID("_InvLogLumRange");
        private static readonly int ID_LogLumRange    = Shader.PropertyToID("_LogLumRange");
        private static readonly int ID_Width          = Shader.PropertyToID("_Width");
        private static readonly int ID_Height         = Shader.PropertyToID("_Height");
        private static readonly int ID_LowPercentile  = Shader.PropertyToID("_LowPercentile");
        private static readonly int ID_HighPercentile = Shader.PropertyToID("_HighPercentile");
        private static readonly int ID_TotalPixels    = Shader.PropertyToID("_TotalPixels");

        private readonly ComputeShader m_CS;
        private readonly Settings      m_Settings;

        private ComputeBuffer m_HistogramBuffer;
        private ComputeBuffer m_AverageLumBuffer;
        private RTHandle      m_SampleRT;

        private float  m_CurrentEV;
        private float  m_TargetLogLum;
        private bool   m_AvgReadbackPending;
        private Volume m_Volume;

        public  bool   DebugEnabled;
        private bool   m_HistReadbackPending;
        private uint[] m_HistogramData;

        // Exposed for debug UI
        public uint[]  HistogramData  => m_HistogramData;
        public float   TargetLogLum   => m_TargetLogLum;
        public float   CurrentEV      => m_CurrentEV;

        private bool m_Disposed;

        private class BlitData    { public TextureHandle source; }
        private class ComputeData { public AutoExposurePass pass; }

        public AutoExposurePass(ComputeShader cs, Settings settings)
        {
            m_CS       = cs;
            m_Settings = settings;

            if (cs != null)
            {
                k_KernelClear     = cs.FindKernel("CSClearHistogram");
                k_KernelHistogram = cs.FindKernel("CSBuildHistogram");
                k_KernelAverage   = cs.FindKernel("CSComputeAverage");
            }

            m_HistogramBuffer  = new ComputeBuffer(k_Bins, sizeof(uint));
            m_AverageLumBuffer = new ComputeBuffer(1, sizeof(float));
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_CS == null || m_Disposed) return;

            // ── CPU: temporal adaptation from last frame's readback ───────────
            if (m_Volume == null) m_Volume = ComponentCache.FindObjectOfType<Volume>();

            float target = Mathf.Clamp(
                m_Settings.exposureCompensation - m_TargetLogLum,
                m_Settings.minEV, m_Settings.maxEV);
            float speed = m_CurrentEV < target ? m_Settings.adaptSpeedUp : m_Settings.adaptSpeedDown;
            m_CurrentEV = Mathf.MoveTowards(m_CurrentEV, target, speed * Time.deltaTime);
            PostEffectsDriver.SetFloat(m_Volume?.profile, PostEffectsDriver.FloatPropertyTarget.Exposure, m_CurrentEV);

            // ── Allocate sample RT ───────────────────────────────────────────
            var desc = new RenderTextureDescriptor(k_SampleW, k_SampleH,
                RenderTextureFormat.DefaultHDR, depthBufferBits: 0)
            {
                sRGB = false, useMipMap = false, autoGenerateMips = false
            };
            RenderingUtils.ReAllocateHandleIfNeeded(
                ref m_SampleRT, desc, FilterMode.Bilinear, TextureWrapMode.Clamp,
                name: "_AutoExposureSample");

            TextureHandle sampleHandle = renderGraph.ImportTexture(m_SampleRT);
            var resourceData = frameData.Get<UniversalResourceData>();

            // ── Pass 1: blit camera color → sample RT ────────────────────────
            using (var builder = renderGraph.AddRasterRenderPass<BlitData>(
                       k_Tag + "_Blit", out var blitData))
            {
                blitData.source = resourceData.activeColorTexture;
                builder.UseTexture(blitData.source, AccessFlags.Read);
                builder.SetRenderAttachment(sampleHandle, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (BlitData data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, data.source,
                        new Vector4(1f, 1f, 0f, 0f), 0f, false);
                });
            }

            // ── Pass 2: clear + histogram + average ──────────────────────────
            using (var builder = renderGraph.AddUnsafePass<ComputeData>(
                       k_Tag + "_Compute", out var compData))
            {
                compData.pass = this;
                builder.UseTexture(sampleHandle, AccessFlags.Read);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (ComputeData data, UnsafeGraphContext ctx) =>
                {
                    CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                    data.pass.DispatchCompute(cmd);
                });
            }
        }

        private void DispatchCompute(CommandBuffer cmd)
        {
            float logLumRange = m_Settings.histogramMaxEV - m_Settings.histogramMinEV;
            float invRange    = logLumRange > 1e-5f ? 1f / logLumRange : 1f;

            // Clear previous frame's histogram
            cmd.SetComputeBufferParam(m_CS, k_KernelClear, ID_Histogram, m_HistogramBuffer);
            cmd.DispatchCompute(m_CS, k_KernelClear, 1, 1, 1);

            // Build histogram
            cmd.SetComputeTextureParam(m_CS, k_KernelHistogram, ID_Source,         m_SampleRT);
            cmd.SetComputeBufferParam (m_CS, k_KernelHistogram, ID_Histogram,      m_HistogramBuffer);
            cmd.SetComputeFloatParam  (m_CS, ID_MinLogLum,      m_Settings.histogramMinEV);
            cmd.SetComputeFloatParam  (m_CS, ID_InvLogLumRange, invRange);
            cmd.SetComputeIntParam    (m_CS, ID_Width,          k_SampleW);
            cmd.SetComputeIntParam    (m_CS, ID_Height,         k_SampleH);
            cmd.DispatchCompute(m_CS, k_KernelHistogram, k_SampleW / 16, k_SampleH / 16, 1);

            // Percentile average → single float
            cmd.SetComputeBufferParam(m_CS, k_KernelAverage, ID_Histogram,     m_HistogramBuffer);
            cmd.SetComputeBufferParam(m_CS, k_KernelAverage, ID_AverageLogLum, m_AverageLumBuffer);
            cmd.SetComputeFloatParam (m_CS, ID_MinLogLum,    m_Settings.histogramMinEV);
            cmd.SetComputeFloatParam (m_CS, ID_LogLumRange,  logLumRange);
            cmd.SetComputeFloatParam (m_CS, ID_LowPercentile,  m_Settings.lowPercentile);
            cmd.SetComputeFloatParam (m_CS, ID_HighPercentile, m_Settings.highPercentile);
            cmd.SetComputeIntParam   (m_CS, ID_TotalPixels,   k_TotalPixels);
            cmd.DispatchCompute(m_CS, k_KernelAverage, 1, 1, 1);

            // Readback requests are recorded into the command buffer so they execute
            // after all preceding compute dispatches have completed on the GPU.
            if (DebugEnabled && !m_HistReadbackPending)
            {
                m_HistReadbackPending = true;
                cmd.RequestAsyncReadback(m_HistogramBuffer, OnHistogramReadback);
            }
            if (!m_AvgReadbackPending)
            {
                m_AvgReadbackPending = true;
                cmd.RequestAsyncReadback(m_AverageLumBuffer, OnAverageReadback);
            }
        }

        private void OnAverageReadback(AsyncGPUReadbackRequest request)
        {
            m_AvgReadbackPending = false;
            if (!request.hasError)
                m_TargetLogLum = request.GetData<float>()[0];
        }

        private void OnHistogramReadback(AsyncGPUReadbackRequest request)
        {
            m_HistReadbackPending = false;
            if (request.hasError) return;
            var data = request.GetData<uint>();
            if (m_HistogramData == null || m_HistogramData.Length != k_Bins)
                m_HistogramData = new uint[k_Bins];
            data.CopyTo(m_HistogramData);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_Disposed = true;
            m_HistogramBuffer?.Release();
            m_AverageLumBuffer?.Release();
            m_SampleRT?.Release();
            m_HistogramBuffer  = null;
            m_AverageLumBuffer = null;
            m_SampleRT         = null;
            m_Volume           = null;
        }
    }
}
