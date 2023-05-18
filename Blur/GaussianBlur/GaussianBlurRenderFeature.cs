using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GaussianBlurRenderFeature : ScriptableRendererFeature
{
    public class GaussianBlurPass : ScriptableRenderPass
    {
        internal static readonly int BlurRadius = Shader.PropertyToID("_BlurOffset");
        internal static readonly int BufferRT1 = Shader.PropertyToID("_BufferRT1");
        internal static readonly int BufferRT2 = Shader.PropertyToID("_BufferRT2");

        private const string PROFILER_TAG = "GaussianBlur";
        RenderTargetIdentifier currentTarget;
        private GaussianBlur _gaussianBlur;

        const int k_MaxPyramidSize = 16;

        internal static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");
        private Material gaussianBlurMat;

        public GaussianBlurPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            Shader gaussianBlurShader = Shader.Find("Hidden/CustomPostEffect/GaussianBlur");
            if (gaussianBlurShader)
            {
                gaussianBlurMat = CoreUtils.CreateEngineMaterial(gaussianBlurShader);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (gaussianBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _gaussianBlur = stack.GetComponent<GaussianBlur>();
            if (_gaussianBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_gaussianBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;

                cmd.BeginSample(PROFILER_TAG);

                int RTWidth = (int) (camera.pixelWidth / _gaussianBlur.RTDownScaling.value);
                int RTHeight = (int) (camera.pixelHeight / _gaussianBlur.RTDownScaling.value);
                cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                cmd.GetTemporaryRT(BufferRT2, RTWidth, RTHeight, 0, FilterMode.Bilinear);

                // downsample screen copy into smaller RT
                cmd.Blit(source, BufferRT1);


                for (int i = 0; i < _gaussianBlur.Iteration.value; i++)
                {
                    // horizontal blur
                    gaussianBlurMat.SetVector(BlurRadius,
                        new Vector4(_gaussianBlur.BlurRadius.value / camera.pixelWidth, 0, 0, 0));
                    cmd.Blit(BufferRT1, BufferRT2, gaussianBlurMat, 0);

                    // vertical blur
                    gaussianBlurMat.SetVector(BlurRadius,
                        new Vector4(0, _gaussianBlur.BlurRadius.value / camera.pixelHeight, 0, 0));
                    cmd.Blit(BufferRT2, BufferRT1, gaussianBlurMat, 0);
                }

                // Render blurred texture in blend pass
                cmd.Blit(BufferRT1, source, gaussianBlurMat, 1);

                // release
                cmd.ReleaseTemporaryRT(BufferRT1);
                cmd.ReleaseTemporaryRT(BufferRT2);

                cmd.EndSample(PROFILER_TAG);
            }
        }

        public void Setup(in RenderTargetIdentifier currentTarget)
        {
            this.currentTarget = currentTarget;
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);
        }
    }

    private GaussianBlurPass _gaussianBlurPass;

    public override void Create()
    {
        _gaussianBlurPass = new GaussianBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _gaussianBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_gaussianBlurPass);
    }
}
