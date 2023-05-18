using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable,VolumeComponentMenu("CustomPostprocess/Blur/RadialBlur")]
public class RadialBlur : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter BlurRadius = new ClampedFloatParameter (0,0,1);
    
    public ClampedIntParameter Iteration = new ClampedIntParameter (10,2,30);

    public ClampedFloatParameter RadialCenterX = new ClampedFloatParameter (0.5f,0,1);

    public ClampedFloatParameter RadialCenterY = new ClampedFloatParameter (0.5f,0,1);

    public bool IsActive()
    {
        return BlurRadius.value > 0;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}
