using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable,VolumeComponentMenu("CustomPostprocess/Blur/DirectionalBlur")]
public class DirectionalBlur : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter BlurRadius = new ClampedFloatParameter (0,0,5);

    public ClampedIntParameter Iteration = new ClampedIntParameter (12,1,30);
    
    public ClampedFloatParameter Angle = new ClampedFloatParameter (0.5f,0,6);

    public ClampedFloatParameter RTDownScaling = new ClampedFloatParameter (1,1,10);

    public bool IsActive()
    {
        return BlurRadius.value > 0;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}
