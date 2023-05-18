using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BokehBlurRenderFeature : ScriptableRendererFeature
{

    public class BokehBlurPass : ScriptableRenderPass
    {
        internal static readonly int GoldenRot = Shader.PropertyToID("_GoldenRot");
        internal static readonly int Params = Shader.PropertyToID("_Params");
        internal static readonly int TempTargetId = Shader.PropertyToID("_TempTargetBokehBlur");
        
        private const string PROFILER_TAG = "BokehBlur";
        private Vector4 mGoldenRot = new Vector4();

        private BokehBlur _bokehBlur;
        private Material bokehBlurMat;
        RenderTargetIdentifier currentTarget;
        public BokehBlurPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            Shader bokehBlurShader=Shader.Find("Hidden/CustomPostEffect/BokehBlur");
            if (bokehBlurShader)
            {
                bokehBlurMat = CoreUtils.CreateEngineMaterial(bokehBlurShader);
            }
            float c = Mathf.Cos(2.39996323f);
            float s = Mathf.Sin(2.39996323f);
            mGoldenRot.Set(c, s, -s, c);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (bokehBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _bokehBlur = stack.GetComponent<BokehBlur>();
            if (_bokehBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_bokehBlur.IsActive())
            {
                var cameraData = renderingData.cameraData;
                var source = currentTarget;
                int destination = TempTargetId;
            
                int RTWidth = (int)(cameraData.camera.scaledPixelWidth  / _bokehBlur.RTDownScaling.value);
                int RTHeight = (int)(cameraData.camera.scaledPixelHeight / _bokehBlur.RTDownScaling.value);
                cmd.GetTemporaryRT(destination, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                bokehBlurMat.SetVector(GoldenRot, mGoldenRot);
                bokehBlurMat.SetVector(Params, new Vector4(_bokehBlur.Iteration.value, _bokehBlur.BlurRadius.value, 1f / cameraData.camera.scaledPixelWidth, 1f / cameraData.camera.scaledPixelHeight));
                cmd.Blit(source,destination,  bokehBlurMat, 0);
                cmd.Blit(destination,source);
                cmd.ReleaseTemporaryRT(destination);
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

    private BokehBlurPass _bokehBlurPass;
    
    public override void Create()
    {
        _bokehBlurPass = new BokehBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _bokehBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_bokehBlurPass);
    }
}
