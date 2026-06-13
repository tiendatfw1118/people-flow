// ─────────────────────────────────────────────────────────────
// SH_PostProcessOutline.shader — Screen-space edge detection
// ─────────────────────────────────────────────────────────────
// Full-screen post-process shader that detects edges using
// depth buffer (Roberts Cross operator) and draws thin outlines.
// Used by OutlineRendererFeature as a URP Renderer Feature.
// ─────────────────────────────────────────────────────────────

Shader "Hidden/PeopleFlow/PostProcessOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.12, 0.10, 0.15, 1.0)
        _OutlineThickness ("Outline Thickness", Range(0.1, 5.0)) = 1.0
        _DepthThreshold ("Depth Threshold", Range(0.0001, 0.01)) = 0.002
        _NormalThreshold ("Normal Threshold", Range(0.0, 1.0)) = 0.4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "PostProcessOutline"

            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // Main screen texture (Blit source)
            TEXTURE2D(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            float4 _BlitTexture_TexelSize;

            // Outline parameters
            float4 _OutlineColor;
            float  _OutlineThickness;
            float  _DepthThreshold;
            float  _NormalThreshold;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                // Full-screen triangle
                float2 uv = float2((input.vertexID << 1) & 2, input.vertexID & 2);
                output.positionCS = float4(uv * 2.0 - 1.0, 0.0, 1.0);

                // Flip Y for D3D
                #if UNITY_UV_STARTS_AT_TOP
                output.uv = float2(uv.x, 1.0 - uv.y);
                #else
                output.uv = uv;
                #endif

                return output;
            }

            // Sample linear eye depth at UV
            float SampleDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                return LinearEyeDepth(rawDepth, _ZBufferParams);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_BlitTexture, uv);

                // Texel offsets for edge detection sampling
                float2 texelSize = _BlitTexture_TexelSize.xy * _OutlineThickness;

                // ─── Roberts Cross Edge Detection on Depth ───
                // Sample 4 neighboring depth values in a 2x2 pattern
                float d00 = SampleDepth(uv + float2(-texelSize.x, -texelSize.y));
                float d11 = SampleDepth(uv + float2( texelSize.x,  texelSize.y));
                float d01 = SampleDepth(uv + float2(-texelSize.x,  texelSize.y));
                float d10 = SampleDepth(uv + float2( texelSize.x, -texelSize.y));

                // Roberts Cross operator — detects diagonal edges
                float depthEdge1 = d00 - d11;
                float depthEdge2 = d01 - d10;

                // Normalize by depth to handle near/far consistently
                float centerDepth = SampleDepth(uv);
                float depthScale = max(centerDepth, 0.1);

                float depthEdge = sqrt(depthEdge1 * depthEdge1 + depthEdge2 * depthEdge2);
                depthEdge = depthEdge / depthScale;

                // Threshold to binary edge
                float edge = step(_DepthThreshold, depthEdge);

                // Don't draw outline on sky/background (very far depth)
                float skyMask = step(centerDepth, 500.0);
                edge *= skyMask;

                // Blend outline color with scene
                float4 outlineCol = float4(_OutlineColor.rgb, 1.0);
                float4 result = lerp(sceneColor, outlineCol, edge * _OutlineColor.a);

                return result;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
