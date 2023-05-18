using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum TiltShiftBlurQualityLevel
{
    High_Quality = 0,
    Normal_Quality = 1,
}

[Serializable]
public sealed class TiltShiftBlurQualityLevelParameter : VolumeParameter<TiltShiftBlurQualityLevel> { }

[Serializable,VolumeComponentMenu("CustomPostprocess/Blur/TiltShiftBlur")]
public class TiltShiftBlur : VolumeComponent, IPostProcessComponent
{
    public TiltShiftBlurQualityLevelParameter QualityLevel = new TiltShiftBlurQualityLevelParameter { value = TiltShiftBlurQualityLevel.High_Quality };
    
    public ClampedFloatParameter AreaSize = new ClampedFloatParameter (0.5f,0,1);
    
    public ClampedFloatParameter BlurRadius = new ClampedFloatParameter (0,0,1);
    
    public ClampedIntParameter Iteration = new ClampedIntParameter (2,1,8);
    
    public ClampedFloatParameter RTDownScaling = new ClampedFloatParameter (1,1,2);

    public bool IsActive()
    {
        return BlurRadius.value > 0;
    }

    public bool IsTileCompatible()
    {
        return false;
    }
}
