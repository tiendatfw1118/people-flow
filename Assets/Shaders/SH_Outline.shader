// ─────────────────────────────────────────────────────────────
// SH_Outline.shader — Inverted Hull Outline for People Flow
// ─────────────────────────────────────────────────────────────
// Applied ONLY to dedicated *_Outliner meshes in the Minion FBX.
// These meshes are pre-scaled slightly larger than the body.
//
// How it works:
//   1. Outline renders at Queue "Geometry-1" (BEFORE body)
//   2. Cull Front → only back-faces rendered → they sit BEHIND body
//   3. Body renders at Queue "Geometry" → covers center via Z-test
//   4. Only the silhouette rim where outliner extends beyond body remains
//
// Usage: Apply as the ONLY material on Character_Outliner and
// Character_Leg_Outliner meshes. NOT on body meshes!
// ─────────────────────────────────────────────────────────────

Shader "PeopleFlow/Outline"
{
    Properties
    {
        _OutlineWidth ("Outline Width (reserved)", Range(0.0, 5.0)) = 0.5
        _OutlineColor ("Outline Color", Color) = (0.12, 0.10, 0.15, 1.0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry-1"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            
            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma multi_compile_instancing
            
            #include "Include/OutlinePass.hlsl"
            ENDHLSL
        }
    }
    
    FallBack Off
}
