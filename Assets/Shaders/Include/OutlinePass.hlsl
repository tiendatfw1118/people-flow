// ─────────────────────────────────────────────────────────────
// OutlinePass.hlsl — Inverted hull outline for People Flow
// ─────────────────────────────────────────────────────────────
// Used on dedicated *_Outliner meshes from the Minion FBX.
// These meshes are pre-scaled slightly larger than the body.
// With Cull Front, only back-faces render → silhouette outline.
// Body renders AFTER (Queue Geometry) and covers the center,
// leaving only the thin rim at edges visible as outline.
// ─────────────────────────────────────────────────────────────

#ifndef OUTLINE_PASS_INCLUDED
#define OUTLINE_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

CBUFFER_START(UnityPerMaterial)
    float  _OutlineWidth;   // Reserved for future fine-tuning
    float4 _OutlineColor;
CBUFFER_END

Varyings OutlineVert(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // Pass through vertex position as-is.
    // The *_Outliner mesh is already pre-scaled by the artist.
    // Cull Front + ZTest LEqual will only show the silhouette rim.
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    
    return output;
}

float4 OutlineFrag(Varyings input) : SV_Target
{
    return _OutlineColor;
}

#endif // OUTLINE_PASS_INCLUDED
