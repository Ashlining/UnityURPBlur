using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DirectionalBlurRenderFeature : ScriptableRendererFeature
{
    private DirectionalBlurPass _directionalBlurPass;
    
    public class DirectionalBlurPass : ScriptableRenderPass
    {
        internal static readonly int Params = Shader.PropertyToID("_Params");
        internal static readonly int BufferRT = Shader.PropertyToID("_BufferRT");
        
        private const string PROFILER_TAG = "DirectionalBlur";

        private DirectionalBlur _directionalBlur;
        private Material directionalBlurMat;
        
        RenderTargetIdentifier currentTarget;
        
        public DirectionalBlurPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            Shader directionalBlurShader=Shader.Find("Hidden/CustomPostEffect/DirectionalBlur");
            if (directionalBlurShader)
            {
                directionalBlurMat = CoreUtils.CreateEngineMaterial(directionalBlurShader);
            }
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (directionalBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _directionalBlur = stack.GetComponent<DirectionalBlur>();
            if (_directionalBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_directionalBlur.IsActive())
            {
                var source = currentTarget;
                int RTWidth = (int)(renderingData.cameraData.camera.pixelWidth / _directionalBlur.RTDownScaling.value);
                int RTHeight = (int)(renderingData.cameraData.camera.pixelHeight / _directionalBlur.RTDownScaling.value);
                cmd.GetTemporaryRT(BufferRT, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                cmd.Blit(source, BufferRT);

                float sinVal = (Mathf.Sin(_directionalBlur.Angle.value) * _directionalBlur.BlurRadius.value * 0.05f) / _directionalBlur.Iteration.value;
                float cosVal = (Mathf.Cos(_directionalBlur.Angle.value) * _directionalBlur.BlurRadius.value * 0.05f) / _directionalBlur.Iteration.value;        
                directionalBlurMat.SetVector(Params, new Vector3(_directionalBlur.Iteration.value, sinVal, cosVal));
                cmd.Blit(BufferRT, source, directionalBlurMat, 0);
                cmd.ReleaseTemporaryRT(BufferRT);
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

    public override void Create()
    {
        _directionalBlurPass = new DirectionalBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _directionalBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_directionalBlurPass);
    }
}
