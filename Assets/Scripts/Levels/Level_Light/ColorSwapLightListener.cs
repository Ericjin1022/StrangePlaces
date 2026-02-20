using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ColorSwapLightListener : BaseColorSwapListener
{
    [SerializeField] public ColorSwapValue intensity;
    private Light2D _light2D;

    protected override void OnSelfAwake()
    {
        _light2D = GetComponent<Light2D>();
    }

    protected override void OnColorSwapRefresh(bool inverted)
    {
        _light2D.intensity = intensity.FloatValue(inverted);
    }
}
