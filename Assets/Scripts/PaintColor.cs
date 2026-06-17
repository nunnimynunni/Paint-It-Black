using UnityEngine;

public enum PaintColor
{
    Red,
    Blue,
    Yellow,
    Green,
    Purple,
    Gray
}

public static class PaintColorUtils
{
    public static Color ToUnityColor(PaintColor color)
    {
        switch (color)
        {
            case PaintColor.Red:  return new Color(0.690f, 0.145f, 0.145f);
            case PaintColor.Blue: return new Color(0.114f, 0.169f, 0.639f);
            case PaintColor.Yellow: return new Color(1f,    0.95f, 0.1f);
            case PaintColor.Green:  return new Color(0.106f, 0.588f, 0.235f);
            case PaintColor.Purple: return new Color(0.518f, 0.153f, 0.659f);
            case PaintColor.Gray:   return new Color(0.6f,  0.7f,  0.75f);
            default:                return Color.white;
        }
    }
}
