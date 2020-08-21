using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class PostProcessorRenderPass : UnityEngine.Rendering.Universal.ScriptableRendererFeature
{
    class PostProcessorPass : UnityEngine.Rendering.Universal.ScriptableRenderPass
    {
        private RenderTextureDescriptor cameraTextureDescriptor;
        private PostProcessorSettings Settings;

        private RenderTargetIdentifier SCREEN_TARGET;
        private static int BLUR_DIR_LWRP = Shader.PropertyToID("_LWRP_BlurDir");

        //Start of post processing
        private int FirstScreenCopy;

        //Vignette
        private int VignetteTarget;

        //Wholescreen blur
        private int WholescreenBlurTarget;

        //Color grading
        private int ColorGradingTarget, ToneMappingTarget, LUTTarget, CHBTarget;

        //Bloom
        private int BloomTarget;

        //command buffer
        private CommandBuffer buffer;

        /// <summary>
        /// Inform about lack of settings user only once to avoid spamming same warning over and over again in console
        /// </summary>
        private bool InformedAboutLackOfSettings = false;
        
        public void SetSettings(PostProcessorSettings Settings) {
            this.Settings = Settings;
        }

        public void SetScreenTarget(RenderTargetIdentifier screenTarget) {
            this.SCREEN_TARGET = screenTarget;
        }

        private List<int> TRT = new List<int>();
        private int GetTRT(string name, CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            int trti = Shader.PropertyToID(name);
            cmd.GetTemporaryRT(trti, cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, FilterMode.Bilinear, cameraTextureDescriptor.colorFormat, RenderTextureReadWrite.Default, 1, false, RenderTextureMemoryless.None, false);
            TRT.Add(trti);
            return trti;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            this.cameraTextureDescriptor = cameraTextureDescriptor;

            //if settings are null do nothing
            if (Settings == null)
            {
                //check if allready informed about lack of post processing settings to avoid spamming with same message in log console
                if (!InformedAboutLackOfSettings)
                {
                    Debug.LogWarning("Please attach post processor settings!");
                    InformedAboutLackOfSettings = true;
                }

                return;
            }
            InformedAboutLackOfSettings = false;

            FirstScreenCopy = GetTRT("_FirstScreenCopy", cmd, cameraTextureDescriptor);

            if(Settings.VignetteEnabled)
                VignetteTarget = GetTRT("_VignetteTarget", cmd, cameraTextureDescriptor);

            if(Settings.BlurEnabled)
                WholescreenBlurTarget = GetTRT("_FullscreenBlurTarget", cmd, cameraTextureDescriptor);
               
            if(Settings.BloomEnabled)
                BloomTarget = GetTRT("_BloomTarget", cmd, cameraTextureDescriptor);

            if (Settings.ColorGradingEnabled) {
                ColorGradingTarget = GetTRT("_ColorGradingTraget", cmd, cameraTextureDescriptor);

                if(Settings.ToneMappingEnabled) 
                    ToneMappingTarget = GetTRT("_ToneMappingTarget", cmd, cameraTextureDescriptor);
                if (Settings.LUTEnabled)
                    LUTTarget = GetTRT("_LUTTarget", cmd, cameraTextureDescriptor);
                if (Settings.ChromaticAbberationEnabled)
                    CHBTarget = GetTRT("_CHBTarget", cmd, cameraTextureDescriptor);
            }
        }
        
        public override void Execute(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData) {

            buffer = GetBuffer(context, ref renderingData);
            
            if (buffer != null)
                context.ExecuteCommandBuffer(buffer);
        }
        
        public CommandBuffer GetBuffer(ScriptableRenderContext context, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
        {
            if(buffer == null)
            {
                buffer = new CommandBuffer();
            }

            buffer.Clear();//CommandBuffer buffer = new CommandBuffer();//CommandBufferPool.Get("Mobile Optimized Post Processing");
            buffer.name = "MOPP";

            //if settings are null do nothing
            if (Settings == null)
            {
                //check if allready informed about lack of post processing settings to avoid spamming with same message in log console
                if (!InformedAboutLackOfSettings)
                {
                    Debug.LogWarning("Please attach post processor settings!");
                    InformedAboutLackOfSettings = true;
                }

                return null;
            }
            InformedAboutLackOfSettings = false;

            //Start post processing process by first coping without any effect to second buffer
            Blit(buffer, SCREEN_TARGET, FirstScreenCopy);
            int currentTarget = FirstScreenCopy;

            //Apply bloom effect
            if (Settings.BloomEnabled)
            {
                ApplyBloom(buffer, currentTarget, BloomTarget);
                currentTarget = BloomTarget;
            }

            //Apply color grading effects
            if (Settings.ColorGradingEnabled)
            {
                ApplyColorGrading(buffer, currentTarget, ColorGradingTarget);
                currentTarget = ColorGradingTarget;
            }

            //Apply vignette effect
            if (Settings.VignetteEnabled)
            {
                Material VignetteMaterial = GetMaterial(PostProcessor.VignetteShader);
                VignetteMaterial.SetVector(PostProcessor.ShaderUniforms.VignetteCenter, Settings.VignetteCenter);
                VignetteMaterial.SetFloat(PostProcessor.ShaderUniforms.VignettePower, Settings.VignettePower);

                Blit(buffer, currentTarget, VignetteTarget, VignetteMaterial);

                currentTarget = VignetteTarget;
            }

            //Apply blur whole screen effect
            if (Settings.BlurEnabled && Settings.BlurRadius > 0)
            {
                BlurSource(buffer, currentTarget, WholescreenBlurTarget, Settings.BlurRadius, Settings.BlurTextureResolutionDivider, Settings.BlurIterations,  "Wholescreen Blur");
                currentTarget = WholescreenBlurTarget;
            }

            //finally blit to screen texture
            Blit(buffer, currentTarget, SCREEN_TARGET);    
            return buffer;
        }

        private void ApplyBloom(CommandBuffer buffer, int from, int to) {
            //first filter glowing parts of scene
            int FilteredScene = Shader.PropertyToID("_BloomFilteredScene");
            buffer.GetTemporaryRT(FilteredScene, (int) (cameraTextureDescriptor.width / Settings.FilterTextureResolutionDivider), (int) (cameraTextureDescriptor.height / Settings.FilterTextureResolutionDivider), 0, FilterMode.Trilinear);
            
            Material FilterMaterial = GetMaterial(PostProcessor.BloomFilterShader);

            //precompute bloom filter paramters there to save gpu performance
            float knee = Settings.BloomThreshold * Settings.BloomSoftThreshold;
            Vector4 filter = new Vector4();
            filter.x = Settings.BloomThreshold;
            filter.y = filter.x - knee;
            filter.z = 2f * knee;
            filter.w = 0.25f / (knee + 0.00001f);
            FilterMaterial.SetVector(PostProcessor.ShaderUniforms.BloomValuesCombined, filter);
            FilterMaterial.SetFloat(PostProcessor.ShaderUniforms.BloomIntensity, Mathf.GammaToLinearSpace(Settings.BloomIntensity));
            Blit(buffer, from, FilteredScene, FilterMaterial);

            //blur filtered scene image
            int BlurredFilteredScene = Shader.PropertyToID("_BloomBlurredFilteredScene");
            buffer.GetTemporaryRT(BlurredFilteredScene, (int)(cameraTextureDescriptor.width / Settings.FilterTextureResolutionDivider), (int)(cameraTextureDescriptor.height / Settings.FilterTextureResolutionDivider), 0, FilterMode.Trilinear);
           
            BlurSource(buffer, FilteredScene, BlurredFilteredScene, Settings.BloomBlurRadius, Settings.BloomBlurTextureResolutionDivider, Settings.BloomBlurIterations, "BloomFilteredSceneBloom");

            buffer.ReleaseTemporaryRT(FilteredScene);

            if (Settings.BloomDebugView)
            {
                Blit(buffer, BlurredFilteredScene, to); 
                buffer.ReleaseTemporaryRT(BlurredFilteredScene);
            }
            else
            {
                Material CombineMaterial = GetMaterial(PostProcessor.BloomCombineShader);
                
                buffer.SetGlobalTexture(PostProcessor.ShaderUniforms.BloomFilteredTexture_LWRP, BlurredFilteredScene);

                Blit(buffer, from, to, CombineMaterial, 1);
                buffer.ReleaseTemporaryRT(BlurredFilteredScene);
            }
        }

        private void ApplyColorGrading(CommandBuffer cmd, int from, int to) {
		    int currentTarget = from;
		
		    if(Settings.ToneMappingEnabled) {
			    Material ToneMappingMaterial = GetMaterial(PostProcessor.ToneMappingShader);
			    ToneMappingMaterial.SetFloat(PostProcessor.ShaderUniforms.Gamma, Settings.Gamma);
			    ToneMappingMaterial.SetFloat(PostProcessor.ShaderUniforms.Exposure, Settings.Exposure);
			
			    Blit(cmd, currentTarget, ToneMappingTarget, ToneMappingMaterial);
			    currentTarget = ToneMappingTarget;
		    }

		    if(Settings.LUTEnabled) {
			    Material LUTMaterial = GetMaterial(PostProcessor.LUTShader);
			    LUTMaterial.SetFloat(PostProcessor.ShaderUniforms.LUTIntensity, Settings.LUTIntensity);
			    LUTMaterial.SetFloat(PostProcessor.ShaderUniforms.ColorsAmount, Settings.ColorsAmount);
			    LUTMaterial.SetTexture(PostProcessor.ShaderUniforms.LUT, Settings.LUT);

			    Blit(cmd, currentTarget, LUTTarget, LUTMaterial);
			    currentTarget = LUTTarget;
		    }

		    if(Settings.ChromaticAbberationEnabled) {
			    Material CHBMaterial = GetMaterial(PostProcessor.ChromaticAbberationShader);
			    CHBMaterial.SetFloat(PostProcessor.ShaderUniforms.ColorsShiftAmount, Settings.ColorsShiftAmount);
			    CHBMaterial.SetFloat(PostProcessor.ShaderUniforms.FishEyeEffectFactor, Settings.FishEyeEffectFactor);

			    CHBMaterial.SetFloat(PostProcessor.ShaderUniforms.FishEyeStart, Settings.FishEyeEffectStart);
			    CHBMaterial.SetFloat(PostProcessor.ShaderUniforms.FishEyeEnd, Settings.FishEyeEffectEnd);

			    Blit(cmd, currentTarget, CHBTarget, CHBMaterial);
			    currentTarget = CHBTarget;
		    }

		    Blit(cmd, currentTarget, to);
	    }

        private void BlurSource(CommandBuffer command, RenderTargetIdentifier source, RenderTargetIdentifier destination, float blurRadius, float resolutionDivider, int iterations, string sampleName)
        {
            FilterMode filter = FilterMode.Trilinear;
            
            Vector2 BlurDirection1 = new Vector2(blurRadius, 0);
            Vector2 BlurDirection2 = new Vector2(0, blurRadius);
           
            int rtW = (int) (cameraTextureDescriptor.width / (float) resolutionDivider);
            int rtH = (int) (cameraTextureDescriptor.height / (float) resolutionDivider);

            Material BlurMaterial = GetMaterial(PostProcessor.BlurShader);
            command.SetGlobalVector(BLUR_DIR_LWRP, BlurDirection1);

            int blurId = Shader.PropertyToID(sampleName + "-1");
            command.GetTemporaryRT(blurId, rtW, rtH, 0, filter, cameraTextureDescriptor.colorFormat);
            Blit(command, source, blurId, BlurMaterial);

            int rtIndex = 0;
            for (int i = 1; i < iterations; i++)
            {
                //blur pass
                command.SetGlobalVector(BLUR_DIR_LWRP, (i % 2 == 0) ? BlurDirection1 : BlurDirection2);

                int rtId2 = Shader.PropertyToID(sampleName + "" + rtIndex++);
                command.GetTemporaryRT(rtId2, rtW, rtH, 0, filter, cameraTextureDescriptor.colorFormat);
                Blit(command, blurId, rtId2, BlurMaterial);
                command.ReleaseTemporaryRT(blurId);
                blurId = rtId2;
            }

            command.Blit(blurId, destination);
            command.ReleaseTemporaryRT(blurId);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            foreach(int textureID in TRT)
                cmd.ReleaseTemporaryRT(textureID);

            TRT.Clear();
        }

        
        private Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        public Material GetMaterial(string shaderName)
        {
            Material material;
            if (Materials.TryGetValue(shaderName, out material))
            {
                return material; //a
            }
            else
            {
                Shader shader = Shader.Find(shaderName);

                if (shader == null)
                {
                    Debug.LogError("Shader not found (" + shaderName + "), check if missed shader is in Shaders folder if not reimport this package. If this problem occurs only in build try to add all shaders in Shaders folder to Always Included Shaders (Project Settings -> Graphics -> Always Included Shaders)");
                }

                Material NewMaterial = new Material(shader);
                NewMaterial.hideFlags = HideFlags.HideAndDontSave;
                Materials.Add(shaderName, NewMaterial);
                return NewMaterial;
            }
        }
    }

    PostProcessorPass renderPass;
    
    public PostProcessorSettings Settings;
    public UnityEngine.Rendering.Universal.RenderPassEvent Order = UnityEngine.Rendering.Universal.RenderPassEvent.BeforeRenderingPostProcessing;

    public bool Enabled = true;

    public override void Create()
    {
        renderPass = new PostProcessorPass();
        renderPass.renderPassEvent = Order;
    }
    
    public override void AddRenderPasses(UnityEngine.Rendering.Universal.ScriptableRenderer renderer, ref UnityEngine.Rendering.Universal.RenderingData renderingData)
    {
        if (Enabled)
        {
            if (Settings != null)
            {
                renderPass.SetSettings(Settings);
                renderPass.SetScreenTarget(renderer.cameraColorTarget);
                renderer.EnqueuePass(renderPass);
            } else {
                Debug.LogWarning("Please attach post processor settings!");
            }
        }
    }
}


