using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public enum IrisBlurQualityLevel
{
    High_Quality = 0,
    Normal_Quality = 1,
}

[Serializable]
public sealed class IrisBlurQualityLevelParameter : VolumeParameter<IrisBlurQualityLevel> { }

[Serializable,VolumeComponentMenu("CustomPostprocess/Blur/IrisBlur")]
public class IrisBlur : VolumeComponent, IPostProcessComponent
{
    public IrisBlurQualityLevelParameter QualityLevel = new IrisBlurQualityLevelParameter { value = IrisBlurQualityLevel.High_Quality };
    
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
