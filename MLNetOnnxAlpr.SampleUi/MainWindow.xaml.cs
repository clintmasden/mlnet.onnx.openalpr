using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.ML;
using MLNetOnnxAlpr.OnnxObjectDetection;
using MLNetOnnxAlpr.OnnxObjectDetection.ML;
using MLNetOnnxAlpr.OnnxObjectDetection.ML.DataModels;
using MlNetOnnxAlpr.OpenAlprClient;
using MLNetOnnxAlpr.SampleUi.Models;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Window = System.Windows.Window;

namespace MLNetOnnxAlpr.SampleUi
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeModel();

            Start();
        }

        private CancellationTokenSource CancellationTokenSource { get; set; }

        private OnnxOutputParser OutputParser { get; set; }
        private PredictionEngine<ImageInputData, TinyYoloPrediction> TinyYoloPredictionEngine { get; set; }
        private PredictionEngine<ImageInputData, CustomVisionPrediction> CustomVisionPredictionEngine { get; set; }

        private OpenAlprClient OpenAlprClient { get; set; }

        private void InitializeModel()
        {
            var modelsDirectory = Path.Combine(Environment.CurrentDirectory, @"ML\OnnxModels");

            var customVisionExport = Directory.GetFiles(modelsDirectory, "*.zip").FirstOrDefault();

            // custom vision model
            if (customVisionExport != null)
            {
                var customVisionModel = new CustomVisionModel(customVisionExport);
                var modelConfigurator = new OnnxModelConfigurator(customVisionModel);

                OutputParser = new OnnxOutputParser(customVisionModel);
                CustomVisionPredictionEngine = modelConfigurator.GetMlNetPredictionEngine<CustomVisionPrediction>();
            }
            else // default model
            {
                var tinyYoloModel = new TinyYoloModel(Path.Combine(modelsDirectory, "TinyYolo2_model.onnx"));
                var modelConfigurator = new OnnxModelConfigurator(tinyYoloModel);

                OutputParser = new OnnxOutputParser(tinyYoloModel);
                TinyYoloPredictionEngine = modelConfigurator.GetMlNetPredictionEngine<TinyYoloPrediction>();
            }
        }

        private void Stop()
        {
            CancellationTokenSource?.Cancel();
        }

        private void Start()
        {
            var videoFilePath = string.Empty;

            if (!File.Exists(videoFilePath))
            {
                // file missing
                return;
            }

            CancellationTokenSource = new CancellationTokenSource();

            Task.Run(() => StartVideoParse(videoFilePath, CancellationTokenSource.Token), CancellationTokenSource.Token);
        }

        /// <summary>
        ///     Starts image parse (same as video)
        /// </summary>
        /// <param name="imageFilePath"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task StartImageParse(string imageFilePath, CancellationToken token)
        {
            if (!File.Exists(imageFilePath))
            {
                // file missing
                return;
            }

            await DrawOverlaysFromBitmapDetectedObjects(new Bitmap(imageFilePath), token);
        }

        /// <summary>
        ///     Starts video parse
        /// </summary>
        /// <param name="videoFilePath"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task StartVideoParse(string videoFilePath, CancellationToken token)
        {
            foreach (var bitmap in GetVideoBitmaps(videoFilePath, token))
            {
                await DrawOverlaysFromBitmapDetectedObjects(bitmap, token);
            }
        }

        /// <summary>
        ///     Yield return of bitmaps from a video file (opencvsharp4)
        /// </summary>
        /// <param name="videoFilePath"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private IEnumerable<Bitmap> GetVideoBitmaps(string videoFilePath, CancellationToken token)
        {
            var videoCapture = new VideoCapture(videoFilePath);

            do
            {
                var retrieveMat = videoCapture.RetrieveMat();

                yield return retrieveMat.ToBitmap();

                if (!(videoCapture.PosFrames + 1 < videoCapture.FrameCount))
                {
                    yield break;
                }
            } while (!token.IsCancellationRequested);
        }

        /// <summary>
        ///     Parent method of <see cref="GetObjectsFromModel" /> and <see cref="DrawOverlays" />
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task DrawOverlaysFromBitmapDetectedObjects(Bitmap bitmap, CancellationToken token)
        {
            var frame = new ImageInputData { Image = bitmap };
            var boundingBoxes = GetObjectsFromModel(frame);

            if (!token.IsCancellationRequested)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    VideoImage.Source = bitmap.ToBitmapImage();
                    DrawOverlays(bitmap, boundingBoxes);
                });
            }
        }

        /// <summary>
        ///     Gets bounding boxes/objects from bitmap
        /// </summary>
        /// <param name="imageInputData"></param>
        /// <returns></returns>
        private List<BoundingBox> GetObjectsFromModel(ImageInputData imageInputData)
        {
            var labels = CustomVisionPredictionEngine?.Predict(imageInputData).PredictedLabels ?? TinyYoloPredictionEngine?.Predict(imageInputData).PredictedLabels;
            var boundingBoxes = OutputParser.ParseOutputs(labels);
            var filteredBoxes = OutputParser.FilterBoundingBoxes(boundingBoxes, 5, 0.5f);
            return filteredBoxes;
        }

        /// <summary>
        ///     Draws bounding boxes information into video canvas
        /// </summary>
        /// <param name="bitmap"></param>
        /// <param name="boundingBoxes"></param>
        private void DrawOverlays(Bitmap bitmap, List<BoundingBox> boundingBoxes)
        {
            VideoCanvas.Children.Clear();

            var boundingBoxOverlays = new List<BoundingBoxOverlay>();

            foreach (var box in boundingBoxes)
            {
                var boundingBoxOverlay = new BoundingBoxOverlay(box, VideoImage.ActualHeight, VideoImage.ActualWidth, bitmap.Height, bitmap.Width);

                boundingBoxOverlay.LicensePlate = GetLicensePlateWithOpenAlprClient(bitmap.Cropped(boundingBoxOverlay.BitmapRectangle));

                // remove already drawn overlays on the same/detected object
                foreach (var overlay in boundingBoxOverlays
                    .Where(overlay => overlay.VisualRectangle.IntersectsWith(boundingBoxOverlay.VisualRectangle))
                    .Where(overlay => box.Confidence > overlay.BoundingBox.Confidence || boundingBoxOverlay.HasLicensePlate && !overlay.HasLicensePlate))
                {
                    VideoCanvas.Children.Remove(overlay.OverlayRectangle);
                    VideoCanvas.Children.Remove(overlay.OverlayDescription);
                    VideoCanvas.Children.Remove(overlay.OverlayRectangleBackground);
                }

                VideoCanvas.Children.Add(boundingBoxOverlay.OverlayRectangleBackground);
                VideoCanvas.Children.Add(boundingBoxOverlay.OverlayDescription);
                VideoCanvas.Children.Add(boundingBoxOverlay.OverlayRectangle);

                boundingBoxOverlays.Add(boundingBoxOverlay);
            }
        }

        /// <summary>
        ///     For debugging
        /// </summary>
        private static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                new Action(delegate { }));
        }

        /// <summary>
        ///     Using Open Alpr dll to get license plate
        /// </summary>
        /// <remarks>
        ///     This is a .NET Framework dll being called from .NET Core [which is bad]
        ///     I've tried to compile a newer version of the source, [creating a vm for legacy dependencies and failed]
        /// </remarks>
        /// <returns></returns>
        private string GetLicensePlateWithOpenAlprClient(Bitmap bitmap)
        {
            OpenAlprClient ??= new OpenAlprClient();

            return OpenAlprClient.GetBestLicensePlate(bitmap);
        }

        /// <summary>
        ///     Using Tesseract to get license plate
        /// </summary>
        /// <remarks>
        ///     The text return is garbled in most circumstances, yet it's here if you wish to explore this option
        /// </remarks>
        /// <returns></returns>
        private string GetLicensePlateWithTesseractEngine(Bitmap bitmap)
        {
            //using (var engine = new TesseractEngine(@"..\tessdata", "eng", EngineMode.Default))
            //{
            //    engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890");

            //    MemoryStream byteStream = new MemoryStream();
            //    bitmap.Save(byteStream, System.Drawing.Imaging.ImageFormat.Tiff);

            //    using (var img = Pix.LoadTiffFromMemory(byteStream.GetBuffer())) //Pix.LoadFromFile(@"C:\Users\user\Desktop\20200716_183957_NF_1_mss6441-car-license.JPG"))
            //    {
            //        using (var page = engine.Process(img))
            //        {
            //            return page.GetText();
            //        }
            //    }
            //}

            return string.Empty;
        }
    }

    internal static class CustomExtensions
    {
        internal static Bitmap ToBitmap(this BitmapImage bitmapImage)
        {
            using var outStream = new MemoryStream();
            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmapImage));
            enc.Save(outStream);
            var bitmap = new Bitmap(outStream);

            return new Bitmap(bitmap);
        }

        internal static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Bmp);
            var image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        internal static Bitmap Cropped(this Bitmap bitmap, Rectangle rectangle)
        {
            var croppedBitmap = new Bitmap(rectangle.Width, rectangle.Height);

            using var g = Graphics.FromImage(croppedBitmap);
            g.DrawImage(bitmap, 0, 0, rectangle, GraphicsUnit.Pixel);

            return croppedBitmap;
        }
    }
}