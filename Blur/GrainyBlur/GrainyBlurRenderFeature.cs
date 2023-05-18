using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class GrainyBlurRenderFeature : ScriptableRendererFeature
{
    public class GrainyBlurPass : ScriptableRenderPass
    {
        internal static readonly int Params = Shader.PropertyToID("_Params");
        internal static readonly int BufferRT = Shader.PropertyToID("_BufferRT");

        private const string PROFILER_TAG = "GrainyBlur";
        RenderTargetIdentifier currentTarget;
        private GrainyBlur _grainyBlur;
        private Material grainyBlurMat;

        public GrainyBlurPass(RenderPassEvent evet)
        {
            renderPassEvent = evet;
            Shader grainyBlurShader = Shader.Find("Hidden/CustomPostEffect/GrainyBlur");
            if (grainyBlurShader)
            {
                grainyBlurMat = CoreUtils.CreateEngineMaterial(grainyBlurShader);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (grainyBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _grainyBlur = stack.GetComponent<GrainyBlur>();
            if (_grainyBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_grainyBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;

                cmd.BeginSample(PROFILER_TAG);

                int RTWidth = (int) (camera.pixelWidth / _grainyBlur.RTDownScaling.value);
                int RTHeight = (int) (camera.pixelHeight / _grainyBlur.RTDownScaling.value);
                cmd.GetTemporaryRT(BufferRT, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                // downsample screen copy into smaller RT
                cmd.Blit(source, BufferRT);

                grainyBlurMat.SetVector(Params,
                    new Vector2(_grainyBlur.BlurRadius.value / camera.pixelHeight, _grainyBlur.Iteration.value));

                cmd.Blit(BufferRT, source, grainyBlurMat, 0);

                cmd.ReleaseTemporaryRT(BufferRT);
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

    private GrainyBlurPass _grainyBlurPass;

    public override void Create()
    {
        _grainyBlurPass = new GrainyBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _grainyBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_grainyBlurPass);
    }
}
