namespace UnityEngine.Rendering.Universal
{
    internal class PostProcessUrpPass : ScriptableRenderPass
    {
        public Material material;
        public FilterMode filterMode { get; set; }

        private RenderTargetIdentifier source;
        private RenderTargetIdentifier blurTemp = new RenderTargetIdentifier(blurTempString);
        private RenderTargetIdentifier blurTemp1 = new RenderTargetIdentifier(blurTemp1String);
        private RenderTargetIdentifier blurTex = new RenderTargetIdentifier(blurTexString);
        private RenderTargetIdentifier tempCopy = new RenderTargetIdentifier(tempCopyString);

        private bool maskSet = false;
        private readonly string tag;
        private readonly int numberOfPasses;
        private readonly bool blur;
        private readonly float blurAmount;
        private readonly Texture2D blurMask; 
        private readonly bool bloom;
        private readonly Color bloomColor;
        private readonly float bloomAmount;
        private readonly float bloomDiffuse;
        private readonly float bloomThreshold;
        private readonly bool lut;
        private readonly Texture2D sourceLut;
        private readonly float lutAmount;
        private readonly int lutDimension;
        private readonly bool imageFiltering;
        private readonly Color color;
        private readonly float contrast;
        private readonly float brightness;
        private readonly float saturation;
        private readonly float exposure;
        private readonly float gamma;
        private readonly float sharpness;
        private readonly bool chromaticAberration;
        private readonly float offset;
        private readonly float fishEyeDistortion;
        private readonly float glitchAmount;
        private readonly bool distortion;
        private readonly float lensDistortion;
        private readonly bool vignette;
        private readonly Color vignetteColor;
        private readonly float vignetteAmount;
        private readonly float vignetteSoftness;

        static readonly int blurTexString = Shader.PropertyToID("_BlurTex");
        static readonly int maskTextureString = Shader.PropertyToID("_MaskTex");
        static readonly int blurAmountString = Shader.PropertyToID("_BlurAmount");
        static readonly int bloomColorString = Shader.PropertyToID("_BloomColor");
        static readonly int blAmountString = Shader.PropertyToID("_BloomAmount");
        static readonly int blDiffuseString = Shader.PropertyToID("_BloomDiffuse");
        static readonly int blThresholdString = Shader.PropertyToID("_BloomThreshold");
        static readonly int lutTexture2DString = Shader.PropertyToID("_LutTex2D");
        static readonly int lutTexture3DString = Shader.PropertyToID("_LutTex3D");
        static readonly int lutAmountString = Shader.PropertyToID("_LutAmount");
        static readonly int colorString = Shader.PropertyToID("_Color");
        static readonly int contrastString = Shader.PropertyToID("_Contrast");
        static readonly int brightnessString = Shader.PropertyToID("_Brightness");
        static readonly int saturationString = Shader.PropertyToID("_Saturation");
        static readonly int exposureString = Shader.PropertyToID("_Exposure");
        static readonly int gammaString = Shader.PropertyToID("_Gamma");
        static readonly int centralFactorString = Shader.PropertyToID("_CentralFactor");
        static readonly int sideFactorString = Shader.PropertyToID("_SideFactor");
        static readonly int offsetString = Shader.PropertyToID("_Offset");
        static readonly int fishEyeString = Shader.PropertyToID("_FishEye");
        static readonly int lensdistortionString = Shader.PropertyToID("_LensDistortion");
        static readonly int vignetteColorString = Shader.PropertyToID("_VignetteColor");
        static readonly int vignetteAmountString = Shader.PropertyToID("_VignetteAmount");
        static readonly int vignetteSoftnessString = Shader.PropertyToID("_VignetteSoftness");
        static readonly int blurTempString = Shader.PropertyToID("_BlurTemp");
        static readonly int blurTemp1String = Shader.PropertyToID("_BlurTemp1");
        static readonly int tempCopyString = Shader.PropertyToID("_TempCopy");

        static readonly string bloomKeyword = "BLOOM";
        static readonly string blurKeyword = "BLUR";
        static readonly string chromaKeyword = "CHROMA";
        static readonly string lutKeyword = "LUT";
        static readonly string filterKeyword = "FILTER";
        static readonly string shaprenKeyword = "SHARPEN";
        static readonly string distortionKeyword = "DISTORTION";

        private int previousLutDimension;
        private Texture2D previous;
        private Texture2D converted2D = null;
        private Texture3D converted3D = null;
        private float t, a;

        public PostProcessUrpPass(RenderPassEvent renderPassEvent, Material material, int numberOfPasses,
            bool blur, float blurAmount, Texture2D blurMask,
            bool bloom, Color bloomColor, float bloomAmount, float bloomDiffuse, float bloomThreshold,
            bool lut, int lutDimension, float lutAmount, Texture2D sourceLut,
            bool imageFiltering, Color color, float contrast, float saturation, float brightness, float exposure, float gamma, float sharpness,
            bool chromaticAberration, float offset, float fishEyeDistortion, float glitchAmount,
            bool distortion, float lensDistortion,
            bool vignette, Color vignetteColor, float vignetteAmount, float vignetteSoftness, string tag)
        {
            this.renderPassEvent = renderPassEvent;
            this.material = material;
            this.numberOfPasses = numberOfPasses;
            this.blur = blur;
            this.blurAmount = blurAmount;
            this.blurMask = blurMask == null ? Texture2D.whiteTexture : blurMask;
            this.bloom = bloom;
            this.bloomColor = bloomColor;
            this.bloomDiffuse = bloomDiffuse;
            this.bloomAmount = bloomAmount;
            this.bloomThreshold = bloomThreshold;
            this.lut = lut;
            this.lutDimension = lutDimension;
            this.lutAmount = lutAmount;
            this.sourceLut = sourceLut;
            this.imageFiltering = imageFiltering;
            this.color = color;
            this.contrast = contrast;
            this.saturation = saturation;
            this.brightness = brightness;
            this.exposure = exposure;
            this.gamma = gamma;
            this.sharpness = sharpness;
            this.chromaticAberration = chromaticAberration;
            this.offset = offset;
            this.fishEyeDistortion = fishEyeDistortion;
            this.glitchAmount = glitchAmount;
            this.distortion = distortion;
            this.lensDistortion = lensDistortion;
            this.vignette = vignette;
            this.vignetteColor = vignetteColor;
            this.vignetteAmount = vignetteAmount;
            this.vignetteSoftness = vignetteSoftness;
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
            cmd.CopyTexture(source, tempCopy);

            if (bloom || blur)
            {

                material.DisableKeyword(blurKeyword);
                material.DisableKeyword(bloomKeyword);
                if (bloom)
                {
                    material.EnableKeyword(bloomKeyword);
                    material.SetColor(bloomColorString, bloomColor);
                    material.SetFloat(blAmountString, bloomAmount);
                    material.SetFloat(blDiffuseString, bloomDiffuse);
                    material.SetFloat(blThresholdString, bloomThreshold);
                }
                if (blur)
                {
                    material.EnableKeyword(blurKeyword);
                    material.SetFloat(blurAmountString, blurAmount);
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
                        cmd.Blit(tempCopy, blurTex, material, bloom && !blur ? 1 : 0);
                    }
                    else if (numberOfPasses == 2)
                    {
                        cmd.GetTemporaryRT(blurTexString, Screen.width / 2, Screen.height / 2, 0, FilterMode.Bilinear);
                        cmd.GetTemporaryRT(blurTempString, Screen.width / 4, Screen.height / 4, 0, FilterMode.Bilinear);
                        cmd.Blit(tempCopy, blurTemp, material, bloom && !blur ? 1 : 0);
                        cmd.Blit(blurTemp, blurTex, material, 0);
                    }
                    else if (numberOfPasses == 3)
                    {
                        cmd.GetTemporaryRT(blurTexString, Screen.width / 4, Screen.height / 4, 0, FilterMode.Bilinear);
                        cmd.GetTemporaryRT(blurTempString, Screen.width / 8, Screen.height / 8, 0, FilterMode.Bilinear);
                        cmd.Blit(tempCopy, blurTex, material, bloom && !blur ? 1 : 0);
                        cmd.Blit(blurTex, blurTemp, material, 0);
                        cmd.Blit(blurTemp, blurTex, material, 0);
                    }
                    else if (numberOfPasses == 4)
                    {
                        cmd.GetTemporaryRT(blurTexString, Screen.width / 4, Screen.height / 4, 0, FilterMode.Bilinear);
                        cmd.GetTemporaryRT(blurTempString, Screen.width / 8, Screen.height / 8, 0, FilterMode.Bilinear);
                        cmd.GetTemporaryRT(blurTemp1String, Screen.width / 16, Screen.height / 16, 0, FilterMode.Bilinear);
                        cmd.Blit(tempCopy, blurTemp, material, bloom && !blur ? 1 : 0);
                        cmd.Blit(blurTemp, blurTemp1, material, 0);
                        cmd.Blit(blurTemp1, blurTemp, material, 0);
                        cmd.Blit(blurTemp, blurTex, material, 0);
                    }
                    else if (numberOfPasses == 5)
                    {
                        cmd.GetTemporaryRT(blurTexString, Screen.width / 4, Screen.height / 4, 0, FilterMode.Bilinear);
                        cmd.GetTemporaryRT(blurTempString, Screen.width / 8, Screen.height / 8, 0, FilterMode.Bilinear);
                        cmd.GetTemporaryRT(blurTemp1String, Screen.width / 16, Screen.height / 16, 0, FilterMode.Bilinear);
                        cmd.Blit(tempCopy, blurTex, material, bloom && !blur ? 1 : 0);
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

                if (lutDimension == 2)
                {
                    material.SetTexture(lutTexture2DString, converted2D);
                }
                else
                {
                    material.SetTexture(lutTexture3DString, converted3D);
                }
            }
            else
            {
                material.DisableKeyword(lutKeyword);
            }


            if (imageFiltering)
            {
                material.EnableKeyword(filterKeyword);
                material.SetColor(colorString, color);
                material.SetFloat(contrastString, contrast + 1f);
                material.SetFloat(brightnessString, brightness * 0.5f + 0.5f);
                material.SetFloat(saturationString, saturation + 1f);
                material.SetFloat(exposureString, exposure);
                material.SetFloat(gammaString, gamma);
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

            cmd.Blit(tempCopy, source, material, lutDimension);

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
            if (previousLutDimension != lutDimension)
            {
                previousLutDimension = lutDimension;
                Convert(sourceLut);
                return;
            }

            if (sourceLut != previous)
            {
                previous = sourceLut;
                Convert(sourceLut);
            }
        }

        private void Convert2D(Texture2D temp2DTex)
        {
            Color[] color = temp2DTex.GetPixels();
            Color[] newCol = new Color[65536];

            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                    for (int x = 0; x < 16; x++)
                        for (int y = 0; y < 16; y++)
                        {
                            float bChannel = (i + j * 16.0f) / 16;
                            int bchIndex0 = Mathf.FloorToInt(bChannel);
                            int bchIndex1 = Mathf.Min(bchIndex0 + 1, 15);
                            float lerpFactor = bChannel - bchIndex0;
                            int index = x + (15 - y) * 256;
                            Color col1 = color[index + bchIndex0 * 16];
                            Color col2 = color[index + bchIndex1 * 16];

                            newCol[x + i * 16 + y * 256 + j * 4096] =
                                Color.Lerp(col1, col2, lerpFactor);
                        }
            if (converted2D)
                Object.DestroyImmediate(converted2D);

            converted2D = new Texture2D(256, 256, TextureFormat.ARGB32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };

            converted2D.SetPixels(newCol);
            converted2D.Apply();
        }

        private void Convert3D(Texture2D temp3DTex)
        {
            var color = temp3DTex.GetPixels();
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
            converted3D.SetPixels(newCol);
            converted3D.Apply();
        }

        private void Convert(Texture2D source)
        {
            if (lutDimension == 2)
            {
                Convert2D(source);
            }
            else
            {
                Convert3D(source);
            }
        }
    }
}
