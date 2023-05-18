using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable,VolumeComponentMenu("CustomPostprocess/Blur/GrainyBlur")]
public class GrainyBlur : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter BlurRadius = new ClampedFloatParameter (0,0,50);
    
    public ClampedIntParameter Iteration = new ClampedIntParameter (4,1,8);

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
