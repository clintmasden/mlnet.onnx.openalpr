using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using MLNetOnnxAlpr.OnnxObjectDetection;
using MLNetOnnxAlpr.OnnxObjectDetection.ML.DataModels;

namespace MLNetOnnxAlpr.SampleUi.Models
{
    public class BoundingBoxOverlay
    {
        private TextBlock _overlayDescription;

        private Rectangle _overlayRectangle;

        private Rectangle _overlayRectangleBackground;

        public BoundingBoxOverlay(BoundingBox box, double visualHeight, double visualWidth, int bitmapHeight, int bitmapWidth)
        {
            BoundingBox = box;
            VisualRectangle = GetRectangleWithAdjustedConstraints(box, visualHeight, visualWidth);
            BitmapRectangle = GetRectangleWithAdjustedConstraints(box, bitmapHeight, bitmapWidth);
        }

        public BoundingBox BoundingBox { get; }

        /// <summary>
        ///     Leveraged for cropping the object from the original bitmap
        /// </summary>
        public System.Drawing.Rectangle BitmapRectangle { get; }

        /// <summary>
        ///     Leveraged for drawing overlays on the video canvas
        /// </summary>
        public System.Drawing.Rectangle VisualRectangle { get; }

        /// <summary>
        ///     Detected license plate from cropped object bitmap
        /// </summary>
        public string LicensePlate { get; set; }

        public bool HasLicensePlate => !string.IsNullOrWhiteSpace(LicensePlate);

        public Color OverlayColor => Color.FromArgb(BoundingBox.BoxColor.A, BoundingBox.BoxColor.R, BoundingBox.BoxColor.G, BoundingBox.BoxColor.B);

        /// <summary>
        ///     The drawn overlay rectangle
        /// </summary>
        public Rectangle OverlayRectangle
        {
            get
            {
                _overlayRectangle ??= new Rectangle
                {
                    Width = VisualRectangle.Width,
                    Height = VisualRectangle.Height,
                    Fill = new SolidColorBrush(Colors.Transparent),
                    Stroke = new SolidColorBrush(OverlayColor),
                    StrokeThickness = 1.0,
                    Margin = new Thickness(VisualRectangle.X, VisualRectangle.Y, 0, 0)
                };

                return _overlayRectangle;
            }
        }

        /// <summary>
        ///     The overlay rectangle background
        /// </summary>
        public Rectangle OverlayRectangleBackground
        {
            get
            {
                _overlayRectangleBackground ??= new Rectangle
                {
                    Width = 134,
                    Height = 29,
                    Fill = new SolidColorBrush(OverlayColor),
                    Margin = new Thickness(VisualRectangle.X, VisualRectangle.Y, 0, 0)
                };

                return _overlayRectangleBackground;
            }
        }

        /// <summary>
        ///     The text block drawn for the overlay rectangle
        /// </summary>
        public TextBlock OverlayDescription
        {
            get
            {
                _overlayDescription ??= new TextBlock
                {
                    Margin = new Thickness(VisualRectangle.X + 4, VisualRectangle.Y + 4, 0, 0),
                    Text = string.IsNullOrWhiteSpace(LicensePlate) ? $"{BoundingBox.Description}" : $"{BoundingBox.Description} - {LicensePlate}",
                    FontWeight = FontWeights.Light,
                    Width = 126,
                    Height = 21,
                    TextAlignment = TextAlignment.Center
                };

                return _overlayDescription;
            }
        }

        /// <summary>
        ///     Gets the rectangle with image constraints
        /// </summary>
        /// <param name="box"></param>
        /// <param name="constraintHeight"></param>
        /// <param name="constraintWidth"></param>
        /// <returns></returns>
        private System.Drawing.Rectangle GetRectangleWithAdjustedConstraints(BoundingBox box, double constraintHeight, double constraintWidth)
        {
            double x = Math.Max(box.Dimensions.X, 0);
            double y = Math.Max(box.Dimensions.Y, 0);
            var width = Math.Min(constraintWidth - x, box.Dimensions.Width);
            var height = Math.Min(constraintHeight - y, box.Dimensions.Height);

            // fit to current image size
            x = constraintWidth * x / ImageSettings.imageWidth;
            y = constraintHeight * y / ImageSettings.imageHeight;
            width = constraintWidth * width / ImageSettings.imageWidth;
            height = constraintHeight * height / ImageSettings.imageHeight;

            return new System.Drawing.Rectangle
            {
                Width = (int)width,
                Height = (int)height,
                X = (int)x,
                Y = (int)y
            };
        }
    }
}