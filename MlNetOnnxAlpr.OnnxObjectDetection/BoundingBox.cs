using System.Drawing;

namespace MLNetOnnxAlpr.OnnxObjectDetection
{
    public class BoundingBoxDimensions
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Height { get; set; }
        public float Width { get; set; }
    }

    public class BoundingBox
    {
        private static readonly Color[] classColors =
        {
            Color.Khaki, Color.Fuchsia, Color.Silver, Color.RoyalBlue,
            Color.Green, Color.DarkOrange, Color.Purple, Color.Gold,
            Color.Red, Color.Aquamarine, Color.Lime, Color.AliceBlue,
            Color.Sienna, Color.Orchid, Color.Tan, Color.LightPink,
            Color.Yellow, Color.HotPink, Color.OliveDrab, Color.SandyBrown,
            Color.DarkTurquoise
        };

        public BoundingBoxDimensions Dimensions { get; set; }

        public string Label { get; set; }

        public float Confidence { get; set; }

        public RectangleF Rectangle => new RectangleF(Dimensions.X, Dimensions.Y, Dimensions.Width, Dimensions.Height);

        public Color BoxColor { get; set; }

        public string Description => $"{Label} ({(Confidence * 100).ToString("0")}%)";

        public static Color GetColor(int index)
        {
            return index < classColors.Length ? classColors[index] : classColors[index % classColors.Length];
        }
    }
}