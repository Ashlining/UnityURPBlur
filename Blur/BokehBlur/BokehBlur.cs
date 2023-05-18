using System;
using System.Collections;
using System.Collections.Generic;
using AmplifyShaderEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable,VolumeComponentMenu("CustomPostprocess/Blur/BokehBlur")]
public class BokehBlur : VolumeComponent, IPostProcessComponent
{
    public BoolParameter EnableBokehBlur = new BoolParameter(false);
    
    public ClampedFloatParameter BlurRadius = new ClampedFloatParameter(1f,0,3);
    
    public ClampedIntParameter Iteration = new ClampedIntParameter(32,8,128);

    public ClampedFloatParameter RTDownScaling = new ClampedFloatParameter(2,1,10);

    public bool IsActive()
    {
        return EnableBokehBlur.value;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}
