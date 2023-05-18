using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DualTentBlurRenderFeature : ScriptableRendererFeature
{
    public class DualTentBlurPass : ScriptableRenderPass
    {
        private const string PROFILER_TAG = "DualTentBlur";
        RenderTargetIdentifier currentTarget;
        private DualTentBlur _dualTentBlur;

        Level[] m_Pyramid;
        const int k_MaxPyramidSize = 16;

        internal static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");
        private Material dualKawaseBlurMat;

        public DualTentBlurPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            Shader dualTentBlurShader = Shader.Find("Hidden/CustomPostEffect/DualTentBlur");
            if (dualTentBlurShader)
            {
                dualKawaseBlurMat = CoreUtils.CreateEngineMaterial(dualTentBlurShader);
            }

            m_Pyramid = new Level[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                m_Pyramid[i] = new Level
                {
                    down = Shader.PropertyToID("_BlurMipDown" + i),
                    up = Shader.PropertyToID("_BlurMipUp" + i)
                };
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (dualKawaseBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _dualTentBlur = stack.GetComponent<DualTentBlur>();
            if (_dualTentBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_dualTentBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);

                int tw = (int) (camera.pixelWidth / _dualTentBlur.RTDownScaling.value);
                int th = (int) (camera.pixelHeight / _dualTentBlur.RTDownScaling.value);

                Vector4 BlurOffsetValue = new Vector4(_dualTentBlur.BlurRadius.value / (float) camera.pixelWidth,
                    _dualTentBlur.BlurRadius.value / (float) camera.pixelHeight, 0, 0);
                dualKawaseBlurMat.SetVector(BlurOffset, BlurOffsetValue);
                // Downsample
                RenderTargetIdentifier lastDown = source;
                for (int i = 0; i < _dualTentBlur.Iteration.value; i++)
                {
                    int mipDown = m_Pyramid[i].down;
                    int mipUp = m_Pyramid[i].up;
                    cmd.GetTemporaryRT(mipDown, tw, th, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(mipUp, tw, th, 0, FilterMode.Bilinear);
                    cmd.Blit(lastDown, mipDown, dualKawaseBlurMat, 0);

                    lastDown = mipDown;
                    tw = Mathf.Max(tw / 2, 1);
                    th = Mathf.Max(th / 2, 1);
                }

                // Upsample
                int lastUp = m_Pyramid[_dualTentBlur.Iteration.value - 1].down;
                for (int i = _dualTentBlur.Iteration.value - 2; i >= 0; i--)
                {
                    int mipUp = m_Pyramid[i].up;
                    cmd.Blit(lastUp, mipUp, dualKawaseBlurMat, 0);
                    lastUp = mipUp;
                }


                // Render blurred texture in blend pass
                cmd.Blit(lastUp, source, dualKawaseBlurMat, 1);

                // Cleanup
                for (int i = 0; i < _dualTentBlur.Iteration.value; i++)
                {
                    if (m_Pyramid[i].down != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].down);
                    if (m_Pyramid[i].up != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].up);
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

        struct Level
        {
            internal int down;
            internal int up;
        }
    }

    private DualTentBlurPass _dualTentBlurPass;

    public override void Create()
    {
        _dualTentBlurPass = new DualTentBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _dualTentBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_dualTentBlurPass);
    }
}
