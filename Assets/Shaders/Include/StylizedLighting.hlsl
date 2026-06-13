// ─────────────────────────────────────────────────────────────
// StylizedLighting.hlsl — Custom lighting for People Flow
// "Cute Cartoon Stylized / Glossy Candy Plastic" art style
// ─────────────────────────────────────────────────────────────
// Used by Shader Graph Custom Function nodes to provide
// cel-shaded lighting with soft shadow colors, rim light,
// and a glossy plastic specular response.
// ─────────────────────────────────────────────────────────────

#ifndef STYLIZED_LIGHTING_INCLUDED
#define STYLIZED_LIGHTING_INCLUDED

// ── Main Light Data Accessor ─────────────────────────────────
// Fetches the main directional light's direction, color, and
// combined distance × shadow attenuation.
void GetMainLight_float(
    float3 WorldPos,
    out float3 Direction,
    out float3 Color,
    out float  Attenuation)
{
#ifdef SHADERGRAPH_PREVIEW
    // In Shader Graph preview, provide default sunlight
    Direction   = normalize(float3(0.5, 0.5, 0.25));
    Color       = float3(1, 1, 1);
    Attenuation = 1;
#else
    Light mainLight = GetMainLight();
    Direction   = mainLight.direction;
    Color       = mainLight.color;
    Attenuation = mainLight.distanceAttenuation * mainLight.shadowAttenuation;
#endif
}

// ── Cel-Shaded Diffuse ───────────────────────────────────────
// Returns a smooth-stepped NdotL for cartoon-style light bands.
// ShadowSoftness controls the width of the transition zone.
void CelShadeDiffuse_float(
    float3 WorldNormal,
    float3 LightDirection,
    float  Attenuation,
    float  ShadowSoftness,
    out float CelShade)
{
    float NdotL = dot(WorldNormal, LightDirection);
    // Remap from [-1,1] to [0,1]
    NdotL = NdotL * 0.5 + 0.5;
    // Apply shadow attenuation
    NdotL *= Attenuation;
    // Smoothstep for soft cel-shade transition
    float edge = 0.45;
    CelShade = smoothstep(edge - ShadowSoftness, edge + ShadowSoftness, NdotL);
}

// ── Stylized Specular ────────────────────────────────────────
// Candy/plastic-style specular with adjustable sharpness.
// Returns a soft, wide specular highlight typical of toy
// plastic surfaces.
void StylizedSpecular_float(
    float3 WorldNormal,
    float3 LightDirection,
    float3 ViewDirection,
    float  Smoothness,
    float  SpecularSize,
    out float Specular)
{
    float3 halfDir = normalize(LightDirection + ViewDirection);
    float  NdotH   = saturate(dot(WorldNormal, halfDir));
    
    // Remap smoothness to specular power (higher = sharper highlight)
    float power = exp2(10.0 * Smoothness + 1.0);
    float spec  = pow(NdotH, power);
    
    // Smoothstep to create a candy-like "soft blob" specular
    Specular = smoothstep(SpecularSize - 0.05, SpecularSize + 0.05, spec);
}

// ── Full Stylized Lighting ───────────────────────────────────
// Combines cel-shaded diffuse, colored shadows, rim light,
// and specular into a complete candy/plastic lighting model.
void StylizedLightingFull_float(
    float3 WorldPos,
    float3 WorldNormal,
    float3 ViewDirection,
    float3 BaseColor,
    float3 ShadowColor,
    float  ShadowSoftness,
    float  Smoothness,
    float3 RimColor,
    float  RimPower,
    float  RimIntensity,
    float  AmbientStrength,
    out float3 FinalColor,
    out float3 Emission)
{
    // ── 1. Get main light ──
    float3 lightDir, lightColor;
    float  attenuation;
    GetMainLight_float(WorldPos, lightDir, lightColor, attenuation);

    // ── 2. Cel-shaded diffuse ──
    float celShade;
    CelShadeDiffuse_float(WorldNormal, lightDir, attenuation, ShadowSoftness, celShade);

    // ── 3. Blend base color with shadow color ──
    // Key insight: shadow is NEVER black — always tinted (purple/blue/warm)
    float3 litColor    = BaseColor * lightColor;
    float3 shadowTint  = ShadowColor * BaseColor * 0.6;
    float3 diffuse     = lerp(shadowTint, litColor, celShade);

    // ── 4. Ambient fill ──
    float3 ambient = BaseColor * AmbientStrength;
    diffuse += ambient;

    // ── 5. Specular (plastic highlight) ──
    float spec;
    StylizedSpecular_float(WorldNormal, lightDir, ViewDirection, Smoothness, 0.5, spec);
    float3 specColor = lightColor * spec * 0.4;
    diffuse += specColor;

    // ── 6. Fresnel rim light ──
    float  fresnel    = pow(1.0 - saturate(dot(WorldNormal, ViewDirection)), RimPower);
    float3 rimContrib = RimColor * fresnel * RimIntensity;

    // ── Output ──
    FinalColor = saturate(diffuse);
    Emission   = rimContrib;
}

#endif // STYLIZED_LIGHTING_INCLUDED
