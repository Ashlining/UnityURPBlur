using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class IrisBlurRenderFeature : ScriptableRendererFeature
{
    public class IrisBlurPass : ScriptableRenderPass
    {
        internal static readonly int Params = Shader.PropertyToID("_Params");
        internal static readonly int BlurredTex = Shader.PropertyToID("_BlurredTex");
        internal static readonly int BufferRT1 = Shader.PropertyToID("_BufferRT1");
        internal static readonly int BufferRT2 = Shader.PropertyToID("_BufferRT2");
        
        private const string PROFILER_TAG = "IrisBlur";
        RenderTargetIdentifier currentTarget;
        private IrisBlur _irisBlur;
        private Material _irisBlurMat;
        
        public IrisBlurPass(RenderPassEvent evet)
        {
            renderPassEvent = evet;
            Shader irisBlurShader=Shader.Find("Hidden/CustomPostEffect/IrisBlur");
            if (irisBlurShader)
            {
                _irisBlurMat = CoreUtils.CreateEngineMaterial(irisBlurShader);
            }
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_irisBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _irisBlur = stack.GetComponent<IrisBlur>();
            if (_irisBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_irisBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;

                cmd.BeginSample(PROFILER_TAG);

                if (_irisBlur.Iteration.value ==1)
                {
                    // Get RT
                    int RTWidth = (int)(camera.pixelWidth / _irisBlur.RTDownScaling.value);
                    int RTHeight = (int)(camera.pixelHeight / _irisBlur.RTDownScaling.value);
                    cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);

                    // Set Property
                    _irisBlurMat.SetVector(Params, new Vector4(_irisBlur.AreaSize.value, _irisBlur.BlurRadius.value));

                    // Do Blit
                   cmd.Blit(source, BufferRT1, _irisBlurMat, (int)_irisBlur.QualityLevel.value);

                    // Final Blit
                    cmd.SetGlobalTexture(BlurredTex, BufferRT1);
                    cmd.Blit(BufferRT1, source, _irisBlurMat, 2);

                    // Release
                    cmd.ReleaseTemporaryRT(BufferRT1);
                }
                else
                {
                    // Get RT
                    int RTWidth = (int)(camera.pixelWidth / _irisBlur.RTDownScaling.value);
                    int RTHeight = (int)(camera.pixelHeight / _irisBlur.RTDownScaling.value);
                    cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(BufferRT2, RTWidth, RTHeight, 0, FilterMode.Bilinear);

                    // Set Property
                    _irisBlurMat.SetVector(Params,new Vector2(_irisBlur.AreaSize.value, _irisBlur.BlurRadius.value));

                    RenderTargetIdentifier finalBlurID = BufferRT1;
                    RenderTargetIdentifier firstID = source;
                    RenderTargetIdentifier secondID = BufferRT1;
                    for (int i = 0; i < _irisBlur.Iteration.value; i++)
                    {
                        // Do Blit
                        cmd.Blit(firstID, secondID, _irisBlurMat, (int)_irisBlur.QualityLevel.value);

                        finalBlurID = secondID;
                        firstID = secondID;
                        secondID = (secondID == BufferRT1) ? BufferRT2 : BufferRT1;
                    }

                    // Final Blit
                    cmd.SetGlobalTexture(BlurredTex, finalBlurID);
                    cmd.Blit(BlurredTex, source, _irisBlurMat, 2);

                    // Release
                    cmd.ReleaseTemporaryRT(BufferRT1);
                    cmd.ReleaseTemporaryRT(BufferRT2);
                }

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

    private IrisBlurPass _irisBlurPass;

    public override void Create()
    {
        _irisBlurPass = new IrisBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _irisBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_irisBlurPass);
    }
}
