using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BoxBlurRenderFeature : ScriptableRendererFeature
{
    private BoxBlurPass _boxBlurPass;

    public class BoxBlurPass : ScriptableRenderPass
    {
        internal static readonly int BlurRadius = Shader.PropertyToID("_BlurOffset");
        internal static readonly int BufferRT1 = Shader.PropertyToID("_BufferRT1");
        internal static readonly int BufferRT2 = Shader.PropertyToID("_BufferRT2");
        internal static readonly int TempTargetId = Shader.PropertyToID("_TempTargetBoxBlur");

        private const string PROFILER_TAG = "BoxBlur";

        private BoxBlur _boxBlur;
        private Material _boxBlurMat;
        RenderTargetIdentifier currentTarget;

        public BoxBlurPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            Shader boxBlurShader = Shader.Find("Hidden/CustomPostEffect/BoxBlur");
            if (boxBlurShader)
            {
                _boxBlurMat = CoreUtils.CreateEngineMaterial(boxBlurShader);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_boxBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _boxBlur = stack.GetComponent<BoxBlur>();
            if (_boxBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            var source = currentTarget;
            int destination = TempTargetId;

            if (_boxBlur.IsActive())
            {
                cmd.BeginSample(PROFILER_TAG);

                int RTWidth = (int) (camera.pixelWidth / _boxBlur.RTDownScaling.value);
                int RTHeight = (int) (camera.pixelHeight / _boxBlur.RTDownScaling.value);
                cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                cmd.GetTemporaryRT(destination, RTWidth, RTHeight, 0, FilterMode.Bilinear);

                cmd.Blit(source, BufferRT1);

                for (int i = 0; i < _boxBlur.Iteration.value; i++)
                {
                    if (_boxBlur.Iteration.value > 20)
                    {
                        return;
                    }

                    Vector4 BlurRadiusValue = new Vector4(_boxBlur.BlurRadius.value / camera.pixelWidth,
                        _boxBlur.BlurRadius.value / camera.pixelHeight, 0, 0);
                    
                    _boxBlurMat.SetVector(BlurRadius, BlurRadiusValue);
                    cmd.Blit(BufferRT1, destination, _boxBlurMat, 0);
                    
                    _boxBlurMat.SetVector(BlurRadius, BlurRadiusValue);
                    cmd.Blit(destination, BufferRT1, _boxBlurMat, 0);
                }
                
                cmd.Blit(BufferRT1, destination, _boxBlurMat, 1);
                cmd.Blit(destination,source);
                
                cmd.ReleaseTemporaryRT(BufferRT1);
                cmd.ReleaseTemporaryRT(BufferRT2);
                cmd.EndSample(PROFILER_TAG);
            }
        }

        public void Setup(in RenderTargetIdentifier currentTarget)
        {
            this.currentTarget = currentTarget;
        }
    }


    public override void Create()
    {
        _boxBlurPass = new BoxBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _boxBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_boxBlurPass);
    }
}
