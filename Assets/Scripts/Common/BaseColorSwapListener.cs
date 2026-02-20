using UnityEngine;
using ColorSwapManager2D = StrangePlaces.Level3_ColorSwap.ColorSwapManager2D;

public class BaseColorSwapListener : MonoBehaviour
{
    ColorSwapManager2D _manager;
    protected ColorSwapManager2D manager => _manager ??= ColorSwapManager2D.Instance;

    void Awake()
    {
        OnSelfAwake();
        if (manager != null)
        {
            manager.InvertedChanged += OnInvertedChanged;
            OnColorSwapRefresh(manager.IsInverted);
        }
    }

    void OnDestroy()
    {
        if (manager != null)
        {
            manager.InvertedChanged -= OnInvertedChanged;
        }

        OnSelfDestroy();
    }

    protected virtual void OnSelfAwake() { }
    protected virtual void OnSelfDestroy() { }
    protected virtual void OnInvertedChanged(bool inverted)
    {
        OnColorSwapRefresh(inverted);
    }
    protected virtual void OnColorSwapRefresh(bool inverted) { }
}

[System.Serializable]
public class ColorSwapValue
{
    public string normalValue;
    public string invertValue;

    public int IntValue(bool inverted)
    {
        string val = inverted ? invertValue : normalValue;
        return int.TryParse(val, out var result) ? result : default;
    }
    
    public long LongValue(bool inverted)
    {
        string val = inverted ? invertValue : normalValue;
        return long.TryParse(val, out var result) ? result : default;
    }
    
    public float FloatValue(bool inverted)
    {
        string val = inverted ? invertValue : normalValue;
        return float.TryParse(val, out var result) ? result : default;
    }
    
    public string StringValue(bool inverted)
    {
        string val = inverted ? invertValue : normalValue;
        return val;
    }
    
    public Color ColorValue(bool inverted)
    {
        string val = inverted ? invertValue : normalValue;
        return ColorUtility.TryParseHtmlString(val, out var result) ? result : default;
    }
}
