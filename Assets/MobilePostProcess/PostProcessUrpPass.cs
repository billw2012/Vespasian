namespace UnityEngine.Rendering.Universal
{
    public class PostProcessUrpPass : ScriptableRenderPass
    {
        public Material material => this.settings.blitMaterial;
        public FilterMode filterMode { get; set; }

        private RenderTargetIdentifier source;
        private RenderTargetIdentifier blurTemp = new RenderTargetIdentifier(blurTempString);
        private RenderTargetIdentifier blurTemp1 = new RenderTargetIdentifier(blurTemp1String);
        private RenderTargetIdentifier blurTex = new RenderTargetIdentifier(blurTexString);
        private RenderTargetIdentifier tempCopy = new RenderTargetIdentifier(tempCopyString);

        private bool maskSet = false;
        private int numberOfPasses = 3;
        private readonly string tag;
        private readonly PostProcessUrp.PostProcessSettings settings;

        private bool blur => settings.Blur;
        private float blurAmount => settings.BlurAmount;
        private Texture2D blurMask => settings.BlurMask == null ? Texture2D.whiteTexture : settings.BlurMask;
        private bool bloom => settings.Bloom;
        private Color bloomColor => this.settings.BloomColor;
        private float bloomAmount => this.settings.BloomAmount;
        private float bloomDiffuse => this.settings.BloomDiffuse;
        private float bloomThreshold => this.settings.BloomThreshold;
        private float bloomSoftness => this.settings.BloomSoftness;
        
        private bool lut => this.settings.LUT;
        private float lutAmount => this.settings.LutAmount;
        private Texture2D sourceLut => this.settings.SourceLut;

        private bool imageFiltering => this.settings.ImageFiltering;
        private Color color => this.settings.Color;
        private float contrast => this.settings.Contrast;
        private float brightness => this.settings.Brightness;
        private float saturation => this.settings.Saturation;
        private float exposure => this.settings.Exposure;
        private float gamma => this.settings.Gamma;
        private float sharpness => this.settings.Sharpness;
        
        private bool chromaticAberration => this.settings.ChromaticAberration;
        private float offset => this.settings.Offset;
        private float fishEyeDistortion => this.settings.FishEyeDistortion;
        private float glitchAmount => this.settings.GlitchAmount;

        private bool distortion => this.settings.Distortion;
        private float lensDistortion => this.settings.LensDistortion;

        private bool vignette => this.settings.Vignette;
        private Color vignetteColor => this.settings.VignetteColor;
        private float vignetteAmount => this.settings.VignetteAmount;
        private float vignetteSoftness => this.settings.VignetteSoftness;

        private static readonly int blurTexString = Shader.PropertyToID("_BlurTex");
        private static readonly int maskTextureString = Shader.PropertyToID("_MaskTex");
        private static readonly int blurAmountString = Shader.PropertyToID("_BlurAmount");
        private static readonly int bloomColorString = Shader.PropertyToID("_BloomColor");
        private static readonly int blDiffuseString = Shader.PropertyToID("_BloomDiffuse");
        private static readonly int blDataString = Shader.PropertyToID("_BloomData");
        private static readonly int lutTextureString = Shader.PropertyToID("_LutTex");
        private static readonly int lutAmountString = Shader.PropertyToID("_LutAmount");
        private static readonly int colorString = Shader.PropertyToID("_Color");
        private static readonly int contrastString = Shader.PropertyToID("_Contrast");
        private static readonly int brightnessString = Shader.PropertyToID("_Brightness");
        private static readonly int saturationString = Shader.PropertyToID("_Saturation");
        private static readonly int centralFactorString = Shader.PropertyToID("_CentralFactor");
        private static readonly int sideFactorString = Shader.PropertyToID("_SideFactor");
        private static readonly int offsetString = Shader.PropertyToID("_Offset");
        private static readonly int fishEyeString = Shader.PropertyToID("_FishEye");
        private static readonly int lensdistortionString = Shader.PropertyToID("_LensDistortion");
        private static readonly int vignetteColorString = Shader.PropertyToID("_VignetteColor");
        private static readonly int vignetteAmountString = Shader.PropertyToID("_VignetteAmount");
        private static readonly int vignetteSoftnessString = Shader.PropertyToID("_VignetteSoftness");

        private static readonly int blurTempString = Shader.PropertyToID("_BlurTemp");
        private static readonly int blurTemp1String = Shader.PropertyToID("_BlurTemp1");
        private static readonly int tempCopyString = Shader.PropertyToID("_TempCopy");

        private static readonly string bloomKeyword = "BLOOM";
        private static readonly string blurKeyword = "BLUR";
        private static readonly string chromaKeyword = "CHROMA";
        private static readonly string lutKeyword = "LUT";
        private static readonly string filterKeyword = "FILTER";
        private static readonly string shaprenKeyword = "SHARPEN";
        private static readonly string distortionKeyword = "DISTORTION";

        private Texture2D previous;
        private Texture3D converted3D = null;
        private float t, a, knee;

        public PostProcessUrpPass(PostProcessUrp.PostProcessSettings settings, string tag)
        {
            this.renderPassEvent = settings.Event;
            this.settings = settings;
            this.tag = tag;
        }

        public void Setup(RenderTargetIdentifier source)
        {
            this.source = source;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(tag);
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            cmd.GetTemporaryRT(tempCopyString, opaqueDesc, FilterMode.Bilinear);

            if (SystemInfo.copyTextureSupport == CopyTextureSupport.None)
            {
                cmd.Blit(source, tempCopy);
            }
            else
            {
                cmd.CopyTexture(source, tempCopy);
            }

            if (bloom || blur)
            {

                material.DisableKeyword(blurKeyword);
                material.DisableKeyword(bloomKeyword);
                if (bloom)
                {
                    material.EnableKeyword(bloomKeyword);
                    material.SetColor(bloomColorString, bloomColor * bloomAmount);
                    material.SetFloat(blDiffuseString, bloomDiffuse);
                    numberOfPasses = Mathf.Max(Mathf.CeilToInt(bloomDiffuse * 4), 1);
                    material.SetFloat(blDiffuseString, numberOfPasses > 1 ? (bloomDiffuse * 4 - Mathf.FloorToInt(bloomDiffuse * 4 - 0.001f)) * 0.5f + 0.5f : bloomDiffuse * 4);
                    knee = bloomThreshold * bloomSoftness;
                    material.SetVector(blDataString, new Vector4(bloomThreshold, bloomThreshold - knee, 2f * knee, 1f / (4f * knee + 0.00001f)));
                }
                if (blur)
                {
                    material.EnableKeyword(blurKeyword);
                    numberOfPasses = Mathf.Max(Mathf.CeilToInt(blurAmount * 4), 1);
                    material.SetFloat(blurAmountString, numberOfPasses > 1 ? (blurAmount * 4 - Mathf.FloorToInt(blurAmount * 4 - 0.001f)) * 0.5f + 0.5f : blurAmount * 4);
                    
                    if (!maskSet)
                    {
                        material.SetTexture(maskTextureString, blurMask);
                        maskSet = true;
                    }
                }
                if (blurAmount > 0 || !blur)
                {
                    if (numberOfPasses == 1)
                    {
                        cmd.GetTemporaryRT(blurTexString, Screen.width / 2, Screen.height / 2, 0, FilterMode.Bilinear);
                        cmd.Blit(tempCopy, blurTex, material, 0);
                    }
                    else if (numberOfPasses == 2)
                    {
                        cmd.GetTemporaryRT(blurTexString, Screen.width / 2, Screen.height / 2, 0, FilterMode.Bilinear);
                        cmd.GetTemporaryRT(blurTempString, Screen.width / 4, Screen.height / 4, 0, FilterMode.Bilinear);
                        cmd.Blit(tempCopy, blurTemp, material, 0);
                        cmd.Blit(blurTemp, blurTex, material, 0);
                    }
                    else if (numberOfPasses == 3)
                    {
                        cmd.GetTemporaryRT(blurTexString, Screen.width / 4, Screen.height / 4, 0, FilterMode.Bilinear);
                        cmd.GetTemporaryRT(blurTempString, Screen.width / 8, Screen.height / 8, 0, FilterMode.Bilinear);
                        cmd.Blit(tempCopy, blurTex, material, 0);
                        cmd.Blit(blurTex, blurTemp, material, 0);
                        cmd.Blit(blurTemp, blurTex, material, 0);
                    }
                    else if (numberOfPasses == 4)
                    {
                        cmd.GetTemporaryRT(blurTexString, Screen.width / 4, Screen.height / 4, 0, FilterMode.Bilinear);
                        cmd.GetTemporaryRT(blurTempString, Screen.width / 8, Screen.height / 8, 0, FilterMode.Bilinear);
                        cmd.GetTemporaryRT(blurTemp1String, Screen.width / 16, Screen.height / 16, 0, FilterMode.Bilinear);
                        cmd.Blit(tempCopy, blurTex, material, 0);
                        cmd.Blit(blurTex, blurTemp, material, 0);
                        cmd.Blit(blurTemp, blurTemp1, material, 0);
                        cmd.Blit(blurTemp1, blurTemp, material, 0);
                        cmd.Blit(blurTemp, blurTex, material, 0);
                    }

                    cmd.SetGlobalTexture(blurTexString, blurTex);
                }
                else
                {
                    cmd.SetGlobalTexture(blurTexString, tempCopy);
                }
            }
            else
            {
                material.DisableKeyword(blurKeyword);
                material.DisableKeyword(bloomKeyword);
            }

            if (lut)
            {
                isConverted();
                material.EnableKeyword(lutKeyword);
                material.SetFloat(lutAmountString, lutAmount);
                material.SetTexture(lutTextureString, converted3D);
            }
            else
            {
                material.DisableKeyword(lutKeyword);
            }


            if (imageFiltering)
            {
                material.EnableKeyword(filterKeyword);
                material.SetColor(colorString, (Mathf.Pow(2, exposure) - gamma) * color);
                material.SetFloat(contrastString, contrast + 1f);
                material.SetFloat(brightnessString, brightness * 0.5f - contrast);
                material.SetFloat(saturationString, saturation + 1f);

                if (sharpness > 0)
                {
                    material.EnableKeyword(shaprenKeyword);
                    material.SetFloat(centralFactorString, 1.0f + (3.2f * sharpness));
                    material.SetFloat(sideFactorString, 0.8f * sharpness);
                }
                else
                {
                    material.DisableKeyword(shaprenKeyword);
                }
            }
            else
            {
                material.DisableKeyword(filterKeyword);
                material.DisableKeyword(shaprenKeyword);
            }

            if (chromaticAberration)
            {
                material.EnableKeyword(chromaKeyword);
                if (glitchAmount > 0)
                {
                    t = Time.realtimeSinceStartup;
                    a = (1.0f + Mathf.Sin(t * 6.0f)) * ((0.5f + Mathf.Sin(t * 16.0f) * 0.25f)) * (0.5f + Mathf.Sin(t * 19.0f) * 0.25f) * (0.5f + Mathf.Sin(t * 27.0f) * 0.25f);
                    material.SetFloat(offsetString, 10 * offset + glitchAmount * Mathf.Pow(a, 3.0f) * 200);
                }
                else
                    material.SetFloat(offsetString, 10 * offset);
                material.SetFloat(fishEyeString, 0.1f * fishEyeDistortion);
            }
            else
            {
                material.DisableKeyword(chromaKeyword);
            }

            if (distortion)
            {
                material.SetFloat(lensdistortionString, -lensDistortion);
                material.EnableKeyword(distortionKeyword);
            }
            else
            {
                material.DisableKeyword(distortionKeyword);
            }

            if (vignette)
            {
                material.SetColor(vignetteColorString, vignetteColor);
                material.SetFloat(vignetteAmountString, 1 - vignetteAmount);
                material.SetFloat(vignetteSoftnessString, 1 - vignetteSoftness - vignetteAmount);
            }
            else
            {
                material.SetFloat(vignetteAmountString, 1f);
                material.SetFloat(vignetteSoftnessString, 0.999f);
            }

            cmd.Blit(tempCopy, source, material, 1);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempCopyString);
            cmd.ReleaseTemporaryRT(blurTempString);
            cmd.ReleaseTemporaryRT(blurTemp1String);
            cmd.ReleaseTemporaryRT(blurTexString);
        }

        private void isConverted()
        {
            if (sourceLut != previous)
            {
                previous = sourceLut;
                Convert(sourceLut);
            }
        }

        private void Convert(Texture2D tempTex)
        {
            var color = tempTex.GetPixels();
            var newCol = new Color[color.Length];

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    for (int k = 0; k < 16; k++)
                    {
                        int val = 16 - j - 1;
                        newCol[i + (j * 16) + (k * 256)] = color[k * 16 + i + val * 256];
                    }
                }
            }
            if (converted3D)
                Object.DestroyImmediate(converted3D);
            converted3D = new Texture3D(16, 16, 16, TextureFormat.ARGB32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };
#pragma warning disable UNT0017 // SetPixels invocation is slow
            converted3D.SetPixels(newCol);
#pragma warning restore UNT0017 // SetPixels invocation is slow
            converted3D.Apply();
        }
    }
}
