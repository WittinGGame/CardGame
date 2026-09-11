using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class UIBlurSourceFeature : ScriptableRendererFeature
{
    private CopyPass copyPass;

    public override void Create()
    {
        copyPass = new CopyPass
        {
            renderPassEvent = RenderPassEvent.AfterRenderingTransparents
        };
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        // Capture เฉพาะ Base Camera
        if (renderingData.cameraData.renderType != CameraRenderType.Base)
            return;

        renderer.EnqueuePass(copyPass);
    }

    protected override void Dispose(bool disposing)
    {
        copyPass?.Dispose();
    }

    private class CopyPass : ScriptableRenderPass
    {
        private RTHandle persistentTexture;

        private static readonly int BlurSourceId =
            Shader.PropertyToID("_UIBlurSource");

        private class PassData
        {
            public TextureHandle source;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {
            UniversalResourceData resourceData =
                frameData.Get<UniversalResourceData>();

            UniversalCameraData cameraData =
                frameData.Get<UniversalCameraData>();

            TextureHandle source =
                resourceData.activeColorTexture;

            RenderTextureDescriptor descriptor =
                cameraData.cameraTargetDescriptor;

            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            // Texture นี้อยู่นอก Render Graph
            // จึงใช้ข้าม Base -> Overlay Camera ได้
            RenderingUtils.ReAllocateHandleIfNeeded(
                ref persistentTexture,
                descriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: "_UIBlurSource"
            );

            // นำ persistent texture เข้ามาให้ Render Graph ใช้
            TextureHandle destination =
                renderGraph.ImportTexture(persistentTexture);

            using (var builder =
                renderGraph.AddRasterRenderPass<PassData>(
                    "Copy UI Blur Source",
                    out var passData))
            {
                passData.source = source;

                builder.UseTexture(
                    source,
                    AccessFlags.Read
                );

                builder.SetRenderAttachment(
                    destination,
                    0,
                    AccessFlags.Write
                );

                builder.SetGlobalTextureAfterPass(
                    destination,
                    BlurSourceId
                );

                builder.AllowPassCulling(false);

                builder.SetRenderFunc(
                    (PassData data, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(
                            context.cmd,
                            data.source,
                            new Vector4(1f, 1f, 0f, 0f),
                            0,
                            false
                        );
                    }
                );
            }
        }

        public void Dispose()
        {
            persistentTexture?.Release();
            persistentTexture = null;
        }
    }
}