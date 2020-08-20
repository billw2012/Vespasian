Thank you for buying my asset I hope it will meet your needs :)!
If you have any questions or problems please contact me piotrtplaystore@gmail.com

How to use this asset:
1. Create PostProcessorSettings asset (right-click in project view Create -> Mobile Optimized Post Processing -> PostProcessorSettings) or use example settings
2. Find your Forward Renderer asset (if your project is from template you can find it in Settings folder).
	Then select this forward renderer and in Render Features click plus button and add Post Processor Render Pass.
	Now attach your PostProcessorSettings to just added Post Processor Render Pass (click arrow to see forward renderer components -> select PostProcessorRenderPass ->
	attach your settings to Settings field, also you can find there other settings just like rendering order)
   
   Also you can use premade by me forward renderers (you can find them in LWRP_URP/ExampleScene).

3. If this forward renderer is already selected as renderer in URP settings then you're done!
   If it is not then find your URP profiles settings (you can find them probably in Settings folder) select them and set 0 element of Renderer List to your new Forward Renderer.

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