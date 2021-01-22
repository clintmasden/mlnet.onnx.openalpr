using System.Drawing;
using Microsoft.ML.Transforms.Image;

namespace MLNetOnnxAlpr.OnnxObjectDetection.ML.DataModels
{
    public struct ImageSettings
    {
        public const int imageHeight = 416;
        public const int imageWidth = 416;
    }

    public class ImageInputData
    {
        [ImageType(ImageSettings.imageHeight, ImageSettings.imageWidth)]
        public Bitmap Image { get; set; }
    }
}