using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DualBoxBlurRenderFeature : ScriptableRendererFeature
{
    public class DualBoxBlurPass : ScriptableRenderPass
    {
        internal static readonly int BlurOffset = Shader.PropertyToID("_BlurOffset");

        private const string PROFILER_TAG = "DualBoxBlur";

        private DualBoxBlur _dualBoxBlur;
        private Material _dualBoxBlurMat;
        RenderTargetIdentifier currentTarget;
        
        Level[] m_Pyramid;
        const int k_MaxPyramidSize = 16;
        
        public DualBoxBlurPass(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            Shader dualBoxBlurShader=Shader.Find("Hidden/CustomPostEffect/DualBoxBlur");
            if (dualBoxBlurShader)
            {
                _dualBoxBlurMat = CoreUtils.CreateEngineMaterial(dualBoxBlurShader);
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
            if (_dualBoxBlurMat == null) return;
            if (!renderingData.cameraData.postProcessEnabled) return;
            var stack = VolumeManager.instance.stack;
            _dualBoxBlur = stack.GetComponent<DualBoxBlur>();
            if (_dualBoxBlur == null) return;
            var cmd = CommandBufferPool.Get(PROFILER_TAG);
            Render(cmd, ref renderingData);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (_dualBoxBlur.IsActive())
            {
                var source = currentTarget;
                Camera camera = renderingData.cameraData.camera;
                cmd.BeginSample(PROFILER_TAG);

                int tw = (int) (camera.pixelWidth / _dualBoxBlur.RTDownScaling.value);
                int th = (int) (camera.pixelHeight / _dualBoxBlur.RTDownScaling.value);

                Vector4 BlurOffsetValue = new Vector4(_dualBoxBlur.BlurRadius.value / camera.pixelWidth,
                    _dualBoxBlur.BlurRadius.value / camera.pixelHeight, 0, 0);
                _dualBoxBlurMat.SetVector(BlurOffset, BlurOffsetValue);
                // Downsample
                RenderTargetIdentifier lastDown = source;
                for (int i = 0; i < _dualBoxBlur.Iteration.value; i++)
                {
                    int mipDown = m_Pyramid[i].down;
                    int mipUp = m_Pyramid[i].up;
                    cmd.GetTemporaryRT(mipDown, tw, th, 0, FilterMode.Bilinear);
                    cmd.GetTemporaryRT(mipUp, tw, th, 0, FilterMode.Bilinear);
                    cmd.Blit(lastDown, mipDown, _dualBoxBlurMat, 0);

                    lastDown = mipDown;
                    tw = Mathf.Max(tw / 2, 1);
                    th = Mathf.Max(th / 2, 1);
                }

                // Upsample
                int lastUp = m_Pyramid[_dualBoxBlur.Iteration.value - 1].down;
                for (int i = _dualBoxBlur.Iteration.value - 2; i >= 0; i--)
                {
                    int mipUp = m_Pyramid[i].up;
                    cmd.Blit(lastUp, mipUp, _dualBoxBlurMat, 0);
                    lastUp = mipUp;
                }


                // Render blurred texture in blend pass
                cmd.Blit(lastUp, source, _dualBoxBlurMat, 1);

                // Cleanup
                for (int i = 0; i < _dualBoxBlur.Iteration.value; i++)
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
    
    private DualBoxBlurPass _dualBoxBlurPass;

    public override void Create()
    {
        _dualBoxBlurPass = new DualBoxBlurPass(RenderPassEvent.BeforeRenderingPostProcessing);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _dualBoxBlurPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(_dualBoxBlurPass);
    }
}
