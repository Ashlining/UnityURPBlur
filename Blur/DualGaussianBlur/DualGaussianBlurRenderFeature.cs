using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DualGaussianBlurRenderFeature : ScriptableRendererFeature
{
    private DualGaussianBlurPass _dualGaussianBlurPass;

    public class DualGaussianBlurPass : ScriptableRenderPass
    {
        internal static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");

        Level[] m_Pyramid;
        const int k_MaxPyramidSize = 16;

        private const string PROFILER_TAG = "DualGaussianBlur";

        private DualGaussianBlur _dualGaussianBlur;
        private Material dualGaussianBlurMat;
        RenderTargetIdentifier currentTarget;

        public DualGaussianBlurPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            Shader dualGaussianBlurShader = Shader.Find("Hidden/CustomPostEffect/DualGaussianBlur");
            if (dualGaussianBlurShader)
            {
                dualGaussianBlurMat = CoreUtils.CreateEngineMaterial(dualGaussianBlurShader);
            }

            m_Pyramid = new Level[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                m_Pyramid[i] = new Level
                {
                    down_vertical = Shader.PropertyToID("_BlurMipDownV" + i),
                    down_horizontal = Shader.PropertyToID("_BlurMipDownH" + i),
                    up_vertical = Shader.PropertyToID("_BlurMipUpV" + i),
                    up_horizontal = Shader.PropertyToID("_BlurMipUpH" + i),

                };
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (dualGaussianBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _dualGaussianBlur = stack.GetComponent<DualGaussianBlur>();
            if (_dualGaussianBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_dualGaussianBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);


                int tw = (int) (camera.pixelWidth / _dualGaussianBlur.RTDownScaling.value);
                int th = (int) (camera.pixelHeight / _dualGaussianBlur.RTDownScaling.value);

                Vector4 BlurOffsetValue = new Vector4(_dualGaussianBlur.BlurRadius.value / (float) camera.pixelWidth,
                    _dualGaussianBlur.BlurRadius.value / (float) camera.pixelHeight, 0, 0);
                dualGaussianBlurMat.SetVector(BlurOffset, BlurOffsetValue);
                // Downsample
                RenderTargetIdentifier lastDown = source;
                for (int i = 0; i < _dualGaussianBlur.Iteration.value; i++)
                {
                    int mipDownV = m_Pyramid[i].down_vertical;
                    int mipDowH = m_Pyramid[i].down_horizontal;
                    int mipUpV = m_Pyramid[i].up_vertical;
                    int mipUpH = m_Pyramid[i].up_horizontal;

                    cmd.GetTemporaryRT(mipDownV, tw, th, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(mipDowH, tw, th, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(mipUpV, tw, th, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(mipUpH, tw, th, 0, FilterMode.Bilinear);

                    // horizontal blur
                    dualGaussianBlurMat.SetVector(BlurOffset,
                        new Vector4(_dualGaussianBlur.BlurRadius.value / camera.pixelWidth, 0, 0, 0));
                    cmd.Blit(lastDown, mipDowH, dualGaussianBlurMat, 0);

                    // vertical blur
                    dualGaussianBlurMat.SetVector(BlurOffset,
                        new Vector4(0, _dualGaussianBlur.BlurRadius.value / camera.pixelHeight, 0, 0));
                    cmd.Blit(mipDowH, mipDownV, dualGaussianBlurMat, 0);

                    lastDown = mipDownV;
                    tw = Mathf.Max(tw / 2, 1);
                    th = Mathf.Max(th / 2, 1);
                }

                // Upsample
                int lastUp = m_Pyramid[_dualGaussianBlur.Iteration.value - 1].down_vertical;
                for (int i = _dualGaussianBlur.Iteration.value - 2; i >= 0; i--)
                {

                    int mipUpV = m_Pyramid[i].up_vertical;
                    int mipUpH = m_Pyramid[i].up_horizontal;

                    // horizontal blur
                    dualGaussianBlurMat.SetVector(BlurOffset,
                        new Vector4(_dualGaussianBlur.BlurRadius.value / camera.pixelWidth, 0, 0, 0));
                    cmd.Blit(lastUp, mipUpH, dualGaussianBlurMat, 0);

                    // vertical blur
                    dualGaussianBlurMat.SetVector(BlurOffset,
                        new Vector4(0, _dualGaussianBlur.BlurRadius.value / camera.pixelHeight, 0, 0));
                    cmd.Blit(mipUpH, mipUpV, dualGaussianBlurMat, 0);

                    lastUp = mipUpV;
                }


                // Render blurred texture in blend pass
                cmd.Blit(lastUp, source, dualGaussianBlurMat, 1);

                // Cleanup
                for (int i = 0; i < _dualGaussianBlur.Iteration.value; i++)
                {
                    if (m_Pyramid[i].down_vertical != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].down_vertical);
                    if (m_Pyramid[i].down_horizontal != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].down_horizontal);
                    if (m_Pyramid[i].up_horizontal != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].up_horizontal);
                    if (m_Pyramid[i].up_vertical != lastUp)
                        cmd.ReleaseTemporaryRT(m_Pyramid[i].up_vertical);
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
            internal int down_vertical;
            internal int down_horizontal;
            internal int up_horizontal;
            internal int up_vertical;
        }
    }

    public override void Create()
    {
        _dualGaussianBlurPass = new DualGaussianBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _dualGaussianBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_dualGaussianBlurPass);
    }
}
