using UnityEngine;

namespace StrangePlaces.Level3_ColorSwap
{
    public enum BinaryColor
    {
        Black = 0,
        White = 1,
    }

    public static class BinaryColorUtil
    {
        public static BinaryColor Invert(this BinaryColor c)
        {
            return c == BinaryColor.Black ? BinaryColor.White : BinaryColor.Black;
        }

        public static BinaryColor InvertIf(this BinaryColor c, bool invert)
        {
            return invert ? c.Invert() : c;
        }

        public static Color ToUnityColor(this BinaryColor c)
        {
            return c == BinaryColor.Black ? Color.black : Color.white;
        }
    }
}

