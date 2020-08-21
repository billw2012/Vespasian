Thank you for buying my asset I hope it will meet your needs :)!
If you have any questions or problems please contact me piotrtplaystore@gmail.com

How to use this asset:
1. Create PostProcessorSettings asset (right-click in project view Create -> Mobile Optimized Post Processing -> PostProcessorSettings) or use example settings
2. Attach your PostProcessorSettings to PostProcessorRenderPass (Find asset called PostProcessorRenderer -> click arrow to see its components -> select PostProcessorRenderPass ->
	attach your settings to Settings field, also you can find there other settings just like rendering order)
3. In your Camera selected Renderer Type to Custom and select Post Processor Renderer asset as Renderer Data

How to properly import LUT texture:
To avoid visual artefacts you have to properly import your LUT textures here is how to import settings should look like:
Texture Type - Default
Texture Shape - 2D
sRGB - Uncheck
alpha source - None

Advanced Settings:
    Streaming Mipmaps - Uncheck
    Generate Mipmaps - Uncheck <---- IMPORTANT

Wrap Mode - Clamp
Filter Mode - Bilinear
Format - Automatic
Compression - None   <---- IMPORTANT


I attached example settings and LUT textures in Example Assets folder so you can achieve the effect you want right away. 


You can modify all PostProcessorSettings parameters in realtime from scripts, example code:

public PostProcessorSettings settings;
void Update() {
    settings.VignettePower = Mathf.Sin(Time.time);
    settings.LUTEnabled = true;
}