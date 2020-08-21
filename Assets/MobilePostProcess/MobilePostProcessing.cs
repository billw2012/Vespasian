using UnityEngine;

[ExecuteInEditMode]
public class MobilePostProcessing : MonoBehaviour
{
    [Range(1,5)] 
    public int NumberOfPasses = 3;
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

    public bool LUT = false;
    [Range(2, 3)]
    public int LutDimension = 2;
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
    public float VignetteSoftness = 0.0001f;

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

    static readonly string bloomKeyword = "BLOOM";
    static readonly string blurKeyword = "BLUR";
    static readonly string chromaKeyword = "CHROMA";
    static readonly string lutKeyword = "LUT";
    static readonly string filterKeyword = "FILTER";
    static readonly string shaprenKeyword = "SHARPEN";
    static readonly string distortionKeyword = "DISTORTION";

    public Material material;

    private int previousLutDimension;
    private Texture2D previous;
    private Texture2D converted2D = null;
    private Texture3D converted3D = null;
    private float t, a;

    public void Start()
    {
        previousLutDimension = LutDimension;

        if (BlurMask==null)
        {
            Shader.SetGlobalTexture(maskTextureString, Texture2D.whiteTexture);
        }
        else
            Shader.SetGlobalTexture(maskTextureString, BlurMask);
    }

    public void Update()
    {
        if (previousLutDimension != LutDimension)
        {
            previousLutDimension = LutDimension;
            Convert(SourceLut);
            return;
        }

        if (SourceLut != previous)
        {
            previous = SourceLut;
            Convert(SourceLut);
        }
    }

    private void OnDestroy()
    {
        if (converted2D != null)
        {
            DestroyImmediate(converted2D);
        }
        converted2D = null;
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
            DestroyImmediate(converted2D);
        converted2D = new Texture2D(256, 256, TextureFormat.ARGB32, false);
        converted2D.SetPixels(newCol);
        converted2D.Apply();
        converted2D.wrapMode = TextureWrapMode.Clamp;
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
            DestroyImmediate(converted3D);
        converted3D = new Texture3D(16, 16, 16, TextureFormat.ARGB32, false);
        converted3D.SetPixels(newCol);
        converted3D.Apply();
        converted3D.wrapMode = TextureWrapMode.Clamp;
    }

    private void Convert(Texture2D source)
    {
        if (LutDimension == 2)
        {
            Convert2D(source);
        }
        else
        {
            Convert3D(source);
        }
    }


    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (Blur || Bloom)
        {
            material.DisableKeyword(blurKeyword);
            material.DisableKeyword(bloomKeyword);
            if (Bloom)
            {
                material.EnableKeyword(bloomKeyword);
                material.SetColor(bloomColorString, BloomColor);
                material.SetFloat(blAmountString, BloomAmount);
                material.SetFloat(blDiffuseString, BloomDiffuse);
                material.SetFloat(blThresholdString, BloomThreshold);
            }
            if (Blur) 
            {
                material.EnableKeyword(blurKeyword);
                material.SetFloat(blurAmountString, BlurAmount);
            }

            if (BlurAmount > 0 || !Blur)
            {
                RenderTexture blurTex = null;

                if (NumberOfPasses == 1)
                {
                    blurTex = RenderTexture.GetTemporary(Screen.width / 2, Screen.height / 2, 0, source.format);
                    Graphics.Blit(source, blurTex, material, Bloom && !Blur ? 1 : 0);
                }
                else if (NumberOfPasses == 2)
                {
                    blurTex = RenderTexture.GetTemporary(Screen.width / 2, Screen.height / 2, 0, source.format);
                    var temp1 = RenderTexture.GetTemporary(Screen.width / 4, Screen.height / 4, 0, source.format);
                    Graphics.Blit(source, temp1, material, Bloom && !Blur ? 1 : 0);
                    Graphics.Blit(temp1, blurTex, material, 0);
                    RenderTexture.ReleaseTemporary(temp1);
                }
                else if (NumberOfPasses == 3)
                {
                    blurTex = RenderTexture.GetTemporary(Screen.width / 4, Screen.height / 4, 0, source.format);
                    var temp1 = RenderTexture.GetTemporary(Screen.width / 8, Screen.height / 8, 0, source.format);
                    Graphics.Blit(source, blurTex, material, Bloom && !Blur ? 1 : 0);
                    Graphics.Blit(blurTex, temp1, material, 0);
                    Graphics.Blit(temp1, blurTex, material, 0);
                    RenderTexture.ReleaseTemporary(temp1);
                }              
                else if (NumberOfPasses == 4)
                {
                    blurTex = RenderTexture.GetTemporary(Screen.width / 8, Screen.height / 8, 0, source.format);
                    var temp1 = RenderTexture.GetTemporary(Screen.width / 16, Screen.height / 16, 0, source.format);
                    var temp2 = RenderTexture.GetTemporary(Screen.width / 4, Screen.height / 4, 0, source.format);
                    Graphics.Blit(source, temp2, material, Bloom && !Blur ? 1 : 0);
                    Graphics.Blit(temp2, blurTex, material, 0);
                    Graphics.Blit(blurTex, temp1, material, 0);
                    Graphics.Blit(temp1, blurTex, material, 0);
                    RenderTexture.ReleaseTemporary(temp1);
                    RenderTexture.ReleaseTemporary(temp2);
                }
                else if (NumberOfPasses == 5)
                {
                    blurTex = RenderTexture.GetTemporary(Screen.width / 4, Screen.height / 4, 0, source.format);
                    var temp1 = RenderTexture.GetTemporary(Screen.width / 8, Screen.height / 8, 0, source.format);
                    var temp2 = RenderTexture.GetTemporary(Screen.width / 16, Screen.height / 16, 0, source.format);
                    Graphics.Blit(source, blurTex, material, Bloom && !Blur ? 1 : 0);
                    Graphics.Blit(blurTex, temp1, material, 0);
                    Graphics.Blit(temp1, temp2, material, 0);
                    Graphics.Blit(temp2, temp1, material, 0);
                    Graphics.Blit(temp1, blurTex, material, 0);
                    RenderTexture.ReleaseTemporary(temp1);
                    RenderTexture.ReleaseTemporary(temp2);
                }

                material.SetTexture(blurTexString, blurTex);
                RenderTexture.ReleaseTemporary(blurTex);
            }
            else
            {
                material.SetTexture(blurTexString, source);
            }
        }
        else
        {
            material.DisableKeyword(blurKeyword);
            material.DisableKeyword(bloomKeyword);
        }

        if (LUT)
        {
            material.EnableKeyword(lutKeyword);
            material.SetFloat(lutAmountString, LutAmount);

            if (LutDimension == 2)
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

        if (ImageFiltering)
        {
            material.EnableKeyword(filterKeyword);
            material.SetColor(colorString, Color);
            material.SetFloat(contrastString, Contrast + 1f);
            material.SetFloat(brightnessString, Brightness * 0.5f + 0.5f);
            material.SetFloat(saturationString, Saturation + 1f);
            material.SetFloat(exposureString, Exposure);
            material.SetFloat(gammaString, Gamma);
            if (Sharpness > 0)
            {
                material.EnableKeyword(shaprenKeyword);
                material.SetFloat(centralFactorString, 1.0f + (3.2f * Sharpness));
                material.SetFloat(sideFactorString, 0.8f * Sharpness);
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

        if (ChromaticAberration)
        {
            material.EnableKeyword(chromaKeyword);

            if (GlitchAmount > 0)
            {
                t = Time.realtimeSinceStartup;
                a = (1.0f + Mathf.Sin(t * 6.0f)) * ((0.5f + Mathf.Sin(t * 16.0f) * 0.25f)) * (0.5f + Mathf.Sin(t * 19.0f) * 0.25f) * (0.5f + Mathf.Sin(t * 27.0f) * 0.25f);
                material.SetFloat(offsetString, 10 * Offset + GlitchAmount * Mathf.Pow(a, 3.0f) * 200);
            }
            else
                material.SetFloat(offsetString, 10 * Offset);

            material.SetFloat(fishEyeString, 0.1f * FishEyeDistortion);
        }
        else
        {
            material.DisableKeyword(chromaKeyword);
        }

        if (Distortion)
        {
            material.SetFloat(lensdistortionString, -LensDistortion);
            material.EnableKeyword(distortionKeyword);
        }
        else
        {
            material.DisableKeyword(distortionKeyword);
        }

        if (Vignette)
        {
            material.SetColor(vignetteColorString, VignetteColor);
            material.SetFloat(vignetteAmountString, 1-VignetteAmount);
            material.SetFloat(vignetteSoftnessString, 1-VignetteSoftness - VignetteAmount);
        }
        else
        {
            material.SetFloat(vignetteAmountString, 1f);
            material.SetFloat(vignetteSoftnessString, 0.999f);
        }

        Graphics.Blit(source, destination, material, LutDimension);
    }
}
