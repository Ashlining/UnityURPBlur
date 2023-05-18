using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class KawaseBlurRenderFeature : ScriptableRendererFeature
{
    public class KawaseBlurPass : ScriptableRenderPass
    {
        internal static readonly int BlurRadius = Shader.PropertyToID("_Offset");
        internal static readonly int BufferRT1 = Shader.PropertyToID("_BufferRT1");
        internal static readonly int BufferRT2 = Shader.PropertyToID("_BufferRT2");

        private const string PROFILER_TAG = "KawaseBlur";

        private Material kawaseBlurMat;
        private KawaseBlur _kawaseBlur;
        RenderTargetIdentifier currentTarget;

        public KawaseBlurPass(RenderPassEvent evet)
        {
            renderPassEvent = evet;
            Shader KkwaseBlurShader = Shader.Find("Hidden/CustomPostEffect/KawaseBlur");
            if (KkwaseBlurShader)
            {
                kawaseBlurMat = CoreUtils.CreateEngineMaterial(KkwaseBlurShader);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (kawaseBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _kawaseBlur = stack.GetComponent<KawaseBlur>();
            if (_kawaseBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_kawaseBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);


                int RTWidth = (int) (camera.pixelWidth / _kawaseBlur.RTDownScaling.value);
                int RTHeight = (int) (camera.pixelHeight / _kawaseBlur.RTDownScaling.value);
                cmd.GetTemporaryRT(BufferRT1, RTWidth, RTHeight, 0, FilterMode.Bilinear);
                cmd.GetTemporaryRT(BufferRT2, RTWidth, RTHeight, 0, FilterMode.Bilinear);

                cmd.Blit(source, BufferRT1);

                bool needSwitch = true;
                for (int i = 0; i < _kawaseBlur.Iteration.value; i++)
                {
                    kawaseBlurMat.SetFloat(BlurRadius,
                        i / _kawaseBlur.RTDownScaling.value + _kawaseBlur.BlurRadius.value);
                    cmd.Blit(needSwitch ? BufferRT1 : BufferRT2, needSwitch ? BufferRT2 : BufferRT1, kawaseBlurMat, 0);
                    needSwitch = !needSwitch;
                }


                kawaseBlurMat.SetFloat(BlurRadius,
                    _kawaseBlur.Iteration.value / _kawaseBlur.RTDownScaling.value + _kawaseBlur.BlurRadius.value);
                cmd.Blit(needSwitch ? BufferRT1 : BufferRT2, source, kawaseBlurMat, 0);

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

    private KawaseBlurPass _kawaseBlurPass;

    public override void Create()
    {
        _kawaseBlurPass = new KawaseBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _kawaseBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_kawaseBlurPass);
    }
}
