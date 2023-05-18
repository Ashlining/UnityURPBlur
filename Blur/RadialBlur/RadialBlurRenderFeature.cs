using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RadialBlurRenderFeature : ScriptableRendererFeature
{
    public class RadialBlurPass :ScriptableRenderPass
    {
        internal static readonly int Params = Shader.PropertyToID("_Params");
        internal static readonly int TempID = Shader.PropertyToID("_Temp");
        private const string PROFILER_TAG = "RadialBlur";
        private Material radialBlurMat;
        private RadialBlur radialBlur;
        RenderTargetIdentifier currentTarget;
        
        public RadialBlurPass(RenderPassEvent evet)
        {
            renderPassEvent = evet;
            Shader radialBlurShader=Shader.Find("Hidden/CustomPostEffect/RadialBlur");
            if (radialBlurShader)
            {
                radialBlurMat = CoreUtils.CreateEngineMaterial(radialBlurShader);
            }
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (radialBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            radialBlur = stack.GetComponent<RadialBlur>();
            if (radialBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (radialBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);
                cmd.GetTemporaryRT(TempID,camera.pixelWidth,camera.pixelHeight,0,FilterMode.Bilinear);
                cmd.Blit(source,TempID);
                radialBlurMat.SetVector(Params , new Vector4(radialBlur.BlurRadius.value * 0.02f, radialBlur.Iteration.value, radialBlur.RadialCenterX.value, radialBlur.RadialCenterY.value));
                cmd.Blit(TempID, source, radialBlurMat, 0);
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

    private RadialBlurPass _RadialBlurPass;

    public override void Create()
    {
        _RadialBlurPass = new RadialBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _RadialBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_RadialBlurPass);
    }
}
