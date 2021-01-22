using System.Drawing;
using System.Linq;
using openalprnet;

namespace MlNetOnnxAlpr.OpenAlprClient
{
    /// <summary>
    ///     A wrapper for the underlining dll, i wish i could compile their source yet...
    /// </summary>
    public class OpenAlprClient
    {
        public OpenAlprClient()
        {
            AlprNet = new AlprNet("us", string.Empty, string.Empty)
            {
                TopN = 10
            };
        }

        private AlprNet AlprNet { get; }

        public bool IsLoaded => AlprNet.IsLoaded();

        public string GetBestLicensePlate(Bitmap bitmap)
        {
            var response = AlprNet.Recognize(bitmap);

            return response.Plates.FirstOrDefault()?.BestPlate?.Characters;
        }
    }
}