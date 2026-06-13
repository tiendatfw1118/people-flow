using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

namespace PeopleFlow.Rendering
{
    /// <summary>
    /// Post-processing pass for drawing screen-space outlines based on depth edges.
    /// </summary>
    public class OutlineRendererFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class OutlineSettings
        {
            [Header("Outline Appearance")]
            public Color outlineColor = new Color(0.12f, 0.10f, 0.15f, 0.85f);

            [Range(0.1f, 5.0f)]
            public float outlineThickness = 1.0f;

            [Header("Edge Detection")]
            [Range(0.0001f, 0.01f)]
            public float depthThreshold = 0.002f;

            [Header("Rendering")]
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        }

        public OutlineSettings settings = new OutlineSettings();
        private OutlineRenderPass _pass;
        private Material _material;

        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineThicknessId = Shader.PropertyToID("_OutlineThickness");
        private static readonly int DepthThresholdId = Shader.PropertyToID("_DepthThreshold");

        public override void Create()
        {
            var shader = Shader.Find("Hidden/PeopleFlow/PostProcessOutline");
            if (shader == null)
            {
                Debug.LogWarning("[OutlineFeature] Shader not found!");
                return;
            }

            _material = CoreUtils.CreateEngineMaterial(shader);
            _pass = new OutlineRenderPass(_material);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_material == null || _pass == null) return;

            _material.SetColor(OutlineColorId, settings.outlineColor);
            _material.SetFloat(OutlineThicknessId, settings.outlineThickness);
            _material.SetFloat(DepthThresholdId, settings.depthThreshold);

            _pass.renderPassEvent = settings.renderPassEvent;
            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            CoreUtils.Destroy(_material);
        }

        // ─── Render Pass using Render Graph (Unity 6) ───────────────────

        class OutlineRenderPass : ScriptableRenderPass
        {
            private Material _material;

            public OutlineRenderPass(Material material)
            {
                _material = material;
                ConfigureInput(ScriptableRenderPassInput.Depth);
                requiresIntermediateTexture = true;
            }

            // Unity 6 Render Graph API
            class PassData
            {
                public TextureHandle source;
                public Material material;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (_material == null) return;

                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();

                if (resourceData.isActiveTargetBackBuffer) return;

                var desc = cameraData.cameraTargetDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;

                TextureHandle source = resourceData.activeColorTexture;
                TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, desc, "_OutlineTemp", false);

                // Pass 1: Apply outline effect (source → destination)
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("OutlineEffect", out var passData))
                {
                    passData.source = source;
                    passData.material = _material;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                    });
                }

                // Pass 2: Copy back (destination → source)
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("OutlineCopyBack", out var passData2))
                {
                    passData2.source = destination;
                    passData2.material = null;

                    builder.UseTexture(destination, AccessFlags.Read);
                    builder.SetRenderAttachment(source, 0, AccessFlags.Write);

                    builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
                    {
                        Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1, 1, 0, 0), 0, false);
                    });
                }
            }
        }
    }
}
