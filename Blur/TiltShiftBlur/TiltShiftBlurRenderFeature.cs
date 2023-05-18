using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TiltShiftBlurRenderFeature : ScriptableRendererFeature
{
    public class TiltShiftBlurPass : ScriptableRenderPass
    {
        internal static readonly int Params = Shader.PropertyToID("_Params");
        internal static readonly int BlurredTex = Shader.PropertyToID("_BlurredTex");
        internal static readonly int BufferRT1 = Shader.PropertyToID("_BufferRT1");
        internal static readonly int BufferRT2 = Shader.PropertyToID("_BufferRT2");
        
        private const string PROFILER_TAG = "TiltShiftBlur";
        private Material tiltShiftBlurMat;
        private TiltShiftBlur tiltShiftBlur;
        RenderTargetIdentifier currentTarget;
        
        public TiltShiftBlurPass(RenderPassEvent evet)
        {
            renderPassEvent = evet;
            Shader tiltShiftBlurShader=Shader.Find("Hidden/CustomPostEffect/TiltShiftBlur");
            if (tiltShiftBlurShader)
            {
                tiltShiftBlurMat = CoreUtils.CreateEngineMaterial(tiltShiftBlurShader);
            }
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (tiltShiftBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            tiltShiftBlur = stack.GetComponent<TiltShiftBlur>();
            if (tiltShiftBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (tiltShiftBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);

                if (tiltShiftBlur.Iteration.value == 1)
                {
                    // Get RT
                    int RTWidth = (int)(camera.pixelWidth / tiltShiftBlur.RTDownScaling.value);
                    int RTHeight = (int)(camera.pixelHeight / tiltShiftBlur.RTDownScaling.value);
                    cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                    // Set Property
                    tiltShiftBlurMat.SetVector(Params, new Vector2(tiltShiftBlur.AreaSize.value, tiltShiftBlur.BlurRadius.value));

                    // Do Blit
                    cmd.Blit(source, BufferRT1, tiltShiftBlurMat, (int)tiltShiftBlur.QualityLevel.value);

                    // Final Blit
                    cmd.SetGlobalTexture(BlurredTex, BufferRT1);
                    cmd.Blit(BufferRT1, source, tiltShiftBlurMat, 2);

                    // Release
                    cmd.ReleaseTemporaryRT(BufferRT1);
                }
                else
                {
                    // Get RT
                    int RTWidth = (int)(camera.pixelWidth / tiltShiftBlur.RTDownScaling.value);
                    int RTHeight = (int)(camera.pixelHeight / tiltShiftBlur.RTDownScaling.value);
                    cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(BufferRT2, RTWidth, RTHeight, 0, FilterMode.Bilinear);

                    // Set Property
                    tiltShiftBlurMat.SetVector(Params, new Vector2(tiltShiftBlur.AreaSize.value, tiltShiftBlur.BlurRadius.value));

                    RenderTargetIdentifier finalBlurID = BufferRT1;
                    RenderTargetIdentifier firstID = source;
                    RenderTargetIdentifier secondID = BufferRT1;
                    for (int i = 0; i < tiltShiftBlur.Iteration.value; i++)
                    {
                        // Do Blit
                        cmd.Blit(firstID, secondID, tiltShiftBlurMat, (int)tiltShiftBlur.QualityLevel.value);

                        finalBlurID = secondID;
                        firstID = secondID;
                        secondID = (secondID == BufferRT1) ? BufferRT2 : BufferRT1;
                    }

                    // Final Blit
                    cmd.SetGlobalTexture(BlurredTex, finalBlurID);
                    cmd.Blit(finalBlurID,source, tiltShiftBlurMat, 2);

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

    private TiltShiftBlurPass _tiltShiftBlurPass;
    
    public override void Create()
    {
        _tiltShiftBlurPass = new TiltShiftBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _tiltShiftBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_tiltShiftBlurPass);
    }
}
