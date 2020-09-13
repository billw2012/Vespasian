using UnityEditor;

namespace UnityEngine.Rendering.Universal
{

    public class PostProcessUrp : ScriptableRendererFeature
    {
        public static PostProcessUrp Instance { get; set; }

        [System.Serializable]
        public class PostProcessSettings
        {
            public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;

            public Material blitMaterial = null;

            public bool Blur = false;
            [Range(0, 1)]
            public float BlurAmount = 1f;
            public Texture2D BlurMask;
            public bool Bloom = false;
            public Color BloomColor = Color.white;
            [Range(0, 5)]
            public float BloomAmount = 1f;
            [Range(0, 1)]
            public float BloomDiffuse = 1f;
            [Range(0, 1)]
            public float BloomThreshold = 0f;
            [Range(0, 1)]
            public float BloomSoftness = 0f;

            public bool LUT = false;
            [Range(0, 1)]
            public float LutAmount = 0.0f;
            public Texture2D SourceLut = null;

            public bool ImageFiltering = false;
            public Color Color = Color.white;
            [Range(0, 1)]
            public float Contrast = 0f;
            [Range(-1, 1)]
            public float Brightness = 0f;
            [Range(-1, 1)]
            public float Saturation = 0f;
            [Range(-1, 1)]
            public float Exposure = 0f;
            [Range(-1, 1)]
            public float Gamma = 0f;
            [Range(0, 1)]
            public float Sharpness = 0f;

            public bool ChromaticAberration = false;
            public float Offset = 0;
            [Range(-1, 1)]
            public float FishEyeDistortion = 0;
            [Range(0, 1)]
            public float GlitchAmount = 0;

            public bool Distortion = false;
            [Range(0, 1)]
            public float LensDistortion = 0;

            public bool Vignette = false;
            public Color VignetteColor = Color.black;
            [Range(0, 1)]
            public float VignetteAmount = 0f;
            [Range(0.001f, 1)]
            public float VignetteSoftness = 0.001f;

            public void CopyFrom(PostProcessSettings from)
            {
                this.Event = from.Event;
                this.blitMaterial = from.blitMaterial;
                this.Blur = from.Blur;
                this.BlurAmount = from.BlurAmount;
                this.BlurMask = from.BlurMask;
                this.Bloom = from.Bloom;
                this.BloomColor = from.BloomColor;
                this.BloomAmount = from.BloomAmount;
                this.BloomDiffuse = from.BloomDiffuse;
                this.BloomThreshold = from.BloomThreshold;
                this.BloomSoftness = from.BloomSoftness;
                this.LUT = from.LUT;
                this.LutAmount = from.LutAmount;
                this.SourceLut = from.SourceLut;
                this.ImageFiltering = from.ImageFiltering;
                this.Color = from.Color;
                this.Contrast = from.Contrast;
                this.Brightness = from.Brightness;
                this.Saturation = from.Saturation;
                this.Exposure = from.Exposure;
                this.Gamma = from.Gamma;
                this.Sharpness = from.Sharpness;
                this.ChromaticAberration = from.ChromaticAberration;
                this.Offset = from.Offset;
                this.FishEyeDistortion = from.FishEyeDistortion;
                this.GlitchAmount = from.GlitchAmount;
                this.Distortion = from.Distortion;
                this.LensDistortion = from.LensDistortion;
                this.Vignette = from.Vignette;
                this.VignetteColor = from.VignetteColor;
                this.VignetteAmount = from.VignetteAmount;
                this.VignetteSoftness = from.VignetteSoftness;
            }

            public PostProcessSettings Clone()
            {
                return new PostProcessSettings {
                    Event = Event,
                    blitMaterial = blitMaterial,
                    Blur = Blur,
                    BlurAmount = BlurAmount,
                    BlurMask = BlurMask,
                    Bloom = Bloom,
                    BloomColor = BloomColor,
                    BloomAmount = BloomAmount,
                    BloomDiffuse = BloomDiffuse,
                    BloomThreshold = BloomThreshold,
                    BloomSoftness = BloomSoftness,
                    LUT = LUT,
                    LutAmount = LutAmount,
                    SourceLut = SourceLut,
                    ImageFiltering = ImageFiltering,
                    Color = Color,
                    Contrast = Contrast,
                    Brightness = Brightness,
                    Saturation = Saturation,
                    Exposure = Exposure,
                    Gamma = Gamma,
                    Sharpness = Sharpness,
                    ChromaticAberration = ChromaticAberration,
                    Offset = Offset,
                    FishEyeDistortion = FishEyeDistortion,
                    GlitchAmount = GlitchAmount,
                    Distortion = Distortion,
                    LensDistortion = LensDistortion,
                    Vignette = Vignette,
                    VignetteColor = VignetteColor,
                    VignetteAmount = VignetteAmount,
                    VignetteSoftness = VignetteSoftness
                };
            }
        }

        public PostProcessSettings settings = new PostProcessSettings();

        [System.NonSerialized]
        public PostProcessSettings runtimeSettings = new PostProcessSettings();

        PostProcessUrpPass ppsUrpPass;

        void Awake()
        {
            this.ResetRuntimeSettings();
        }

        void OnValidate()
        {
            this.ResetRuntimeSettings();
        }

        public void ResetRuntimeSettings()
        {
            this.runtimeSettings.CopyFrom(this.settings);
        }

        public override void Create()
        {
            this.ResetRuntimeSettings();
            this.ppsUrpPass = new PostProcessUrpPass(this.runtimeSettings, this.name);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (Instance == null)
            {
                Instance = this;
            }
            ppsUrpPass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(ppsUrpPass);
        }
    }
}

