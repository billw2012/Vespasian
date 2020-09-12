
void PBR_ZeroAttenuation_half(half3 Albedo,
	half Metallic,
	half3 Specular,
	half Smoothness,
	half3 Emission,
	half Alpha,
	float3 PositionWS,
	half3 NormalWS,
	half3 ViewDirectionWS,
	out half4 fragOut)
{
#ifdef UNIVERSAL_LIGHTING_INCLUDED
	BRDFData brdfData;
	InitializeBRDFData(Albedo, Metallic, Specular, Smoothness, Alpha, brdfData);

	Light mainLight = GetMainLight();
	half3 color = LightingPhysicallyBased(brdfData, mainLight, NormalWS, ViewDirectionWS);

	uint pixelLightCount = GetAdditionalLightsCount();
	for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
	{
		Light light = GetAdditionalLight(lightIndex, PositionWS);
		color += LightingPhysicallyBased(brdfData, light.color, light.direction, 1, NormalWS, ViewDirectionWS);
	}

	color += Emission;

	fragOut = half4(color, Alpha);
#else
	fragOut = half4 (1, 1, 1, 1);
#endif
}
