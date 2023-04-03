
using Microsoft.AspNetCore.Mvc;
using OpenCvSharp;
using Sdcb.PaddleInference;
using Sdcb.PaddleOCR.Models.LocalV3;
using Sdcb.PaddleOCR.Models;
using Sdcb.PaddleOCR;
using System.Diagnostics;

namespace paddlesharp_ocr_aspnetcore_demo.Controllers
{
    public class OcrController
    {
        [Route("ocr")]
        public OcrResponse Ocr(IFormFile file)
        {
            PaddleConfig.Defaults.UseMkldnn = true;
            FullOcrModel model = LocalFullModels.ChineseV3;

            using MemoryStream ms = new();
            using Stream stream = file.OpenReadStream();
            stream.CopyTo(ms);
            using Mat src = Cv2.ImDecode(ms.ToArray(), ImreadModes.Color);
            double scale = 1;
            using Mat scaled = src.Resize(Size.Zero, scale, scale);

            Stopwatch sw = Stopwatch.StartNew();
            using PaddleOcrAll all = new(model)
            {
                Enable180Classification = true,
                AllowRotateDetection = true,
            };

            return new OcrResponse(all.Run(scaled).Text, sw.ElapsedMilliseconds);
        }
    }

    public record OcrResponse(string Text, long ElapsedMs);
}
